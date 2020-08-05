# Подгрузка библиотек
import clr

clr.AddReference('RevitAPI')
import Autodesk
from Autodesk.Revit.DB import *

clr.AddReference('RevitServices')
import RevitServices
from RevitServices.Persistence import DocumentManager
from RevitServices.Transactions import TransactionManager

import math

# Получение текущего проекта
doc = DocumentManager.Instance.CurrentDBDocument
uidoc = DocumentManager.Instance.CurrentUIApplication.ActiveUIDocument

# Опции для работы функций
opt = Options() # Для получения геометрии
optS = SolidCurveIntersectionOptions() # Для нахождения пересечения объёмного тела с кривой
nonStr = Autodesk.Revit.DB.Structure.StructuralType.NonStructural # Для вставки семейства в основу

# Входные данные
rectnOpen = UnwrapElement(IN[0]) # Тип прямоугольного проёма для стены
roundOpen = UnwrapElement(IN[1]) # Тип круглого проёма для стены
rectnOpenF = UnwrapElement(IN[2]) # Тип прямоугольного проёма для плиты
roundOpenF = UnwrapElement(IN[3]) # Тип круглого проёма для плиты

# Получение всех экземпляров стен и плит в проекте
walls = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().ToElements()
floors = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Floors).WhereElementIsNotElementType().ToElements()

# Создаём список, в который будем записывать вновь созданные отверстия
lst = []

# Открытие транзакции
TransactionManager.Instance.EnsureInTransaction(doc)

# Формирование отверстий в стенах
for wall in walls:
	# Каждый раз берём фильтр всех окон в проекте
	caps = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType()
	# Получаем геометрию стены
	geomElem = wall.get_Geometry(opt)
	for geomObj in geomElem:
		geomSolid = geomObj	
	# Получаем список окон, которые пересекаются с данной геометрией стены
	inters = caps.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	# Обрабатываем каждое найденное окно
	for inter in inters:
		# Проверяем имя семейства найденного окна
		if inter.Name == 'Заглушка круглая':
			# Проверяем, что экземпляр заглушки принят
			check = inter.LookupParameter('Принято').AsInteger()
			if check == 1:
				# Определяем точку вставки и габариты для круглой заглушки
				point = inter.Location.Point
				openWidth = inter.LookupParameter('Диаметр проёма').AsDouble()
				# Создаём по заглушке отверстие и назначаем габариты
				cutNew = doc.Create.NewFamilyInstance(point, roundOpen, wall, doc.GetElement(wall.LevelId), nonStr)
				cutNew.LookupParameter('Диаметр проёма').Set(openWidth)
				# Определяем и заполняем другие параметры
				date = inter.LookupParameter('Дата').AsString()
				disp = inter.LookupParameter('Дисциплина проёма').AsString()
				cutNew.LookupParameter('Дата').Set(date)
				cutNew.LookupParameter('Дисциплина проёма').Set(disp)
				lst.append(cutNew)
		# Аналогично делаем для прямоугольных проёмов в стене
		elif inter.Name == 'Заглушка прямоугольная':
			check = inter.LookupParameter('Принято').AsInteger()
			if check == 1:
				point = inter.Location.Point
				openWidth = inter.LookupParameter('Ширина проёма').AsDouble()
				openHeight = inter.LookupParameter('Высота проёма').AsDouble()
				cutNew = doc.Create.NewFamilyInstance(point, rectnOpen, wall, doc.GetElement(wall.LevelId), nonStr)
				cutNew.LookupParameter('Ширина проёма').Set(openWidth)
				cutNew.LookupParameter('Высота проёма').Set(openHeight)
				date = inter.LookupParameter('Дата').AsString()
				disp = inter.LookupParameter('Дисциплина проёма').AsString()
				cutNew.LookupParameter('Дата').Set(date)
				cutNew.LookupParameter('Дисциплина проёма').Set(disp)
				lst.append(cutNew)

# Аналогично делаем для проёмом в перекрытиях
for floor in floors:
	caps = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Windows).WhereElementIsNotElementType()
	geomElem = floor.get_Geometry(opt)
	for geomObj in geomElem:
		geomSolid = geomObj	
	inters = caps.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	for inter in inters:	
		if inter.Name == 'Заглушка круглая для плиты':
			check = inter.LookupParameter('Принято').AsInteger()
			if check == 1:
				point = inter.Location.Point
				openWidth = inter.LookupParameter('Диаметр проёма').AsDouble()
				cutNew = doc.Create.NewFamilyInstance(point, roundOpenF, floor, doc.GetElement(floor.LevelId), nonStr)
				cutNew.LookupParameter('Диаметр проёма').Set(openWidth)
				date = inter.LookupParameter('Дата').AsString()
				disp = inter.LookupParameter('Дисциплина проёма').AsString()
				cutNew.LookupParameter('Дата').Set(date)
				cutNew.LookupParameter('Дисциплина проёма').Set(disp)
				lst.append(cutNew)
		elif inter.Name == 'Заглушка прямоугольная для плиты':
			check = inter.LookupParameter('Принято').AsInteger()
			if check == 1:
				point = inter.Location.Point
				openWidth = inter.LookupParameter('Ширина проёма').AsDouble()
				openHeight = inter.LookupParameter('Высота проёма').AsDouble()
				openDepth = inter.LookupParameter('Глубина проёма').AsDouble()
				# Для прямоугольного проёма в плите берём также информация о повороте заглушки
				dirX = inter.LookupParameter('Поворот X').AsDouble()
				dirY = inter.LookupParameter('Поворот Y').AsDouble()
				direction = XYZ(dirX, dirY, 0)
				cutNew = doc.Create.NewFamilyInstance(point, rectnOpenF, floor, doc.GetElement(floor.LevelId), nonStr)
				# Поворачиваем проём на нужный угол после вставки
				# Задаём вертикальную ось поворота
				point1 = point
				point2 = XYZ(point.X, point.Y, point.Z+10)
				axis = Line.CreateBound(point, point2)
				# Определяем угол в радианах
				if dirX >= 0 and dirY >= 0:
					angle = math.acos(dirX)
				elif dirX >= 0 and dirY <= 0:
					angle = math.acos(-dirX)
				elif dirX <= 0 and dirY >= 0:
					angle = math.acos(dirX)
				else:
					angle = math.pi - math.acos(dirX)
				# Поворачиваем по найденной оси на заданный угол
				ElementTransformUtils.RotateElement(doc, cutNew.Id, axis, angle)
				# Далее всё как в случае со стеной
				cutNew.LookupParameter('Ширина проёма').Set(openWidth)
				cutNew.LookupParameter('Высота проёма').Set(openHeight)
				date = inter.LookupParameter('Дата').AsString()
				disp = inter.LookupParameter('Дисциплина проёма').AsString()
				cutNew.LookupParameter('Дата').Set(date)
				cutNew.LookupParameter('Дисциплина проёма').Set(disp)
				lst.append(cutNew)
	
# Закрытие транзакции
TransactionManager.Instance.TransactionTaskDone()

OUT = lst