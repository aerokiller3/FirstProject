# Подгрузка библиотек
import clr
clr.AddReference('ProtoGeometry')
from Autodesk.DesignScript.Geometry import *

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

# Получение списка связанных файлов
linkInstances = FilteredElementCollector(doc).OfClass(RevitLinkInstance)

# Опции для работы функций
opt = Options() # Для получения геометрии
optS = SolidCurveIntersectionOptions() # Для нахождения пересечения объёмного тела с кривой
nonStr = Autodesk.Revit.DB.Structure.StructuralType.NonStructural # Для вставки семейства в основу

# Входные данные
rectnOpen = UnwrapElement(IN[0]) # Тип прямоугольного проёма
roundOpen = UnwrapElement(IN[1]) # Тип круглого проёма
rectnReservType = IN[2] # Тип запаса для прямоугольного проёма
roundReservType = IN[3] # Тип запаса для круглого проёма
if rectnReservType:
	rectnReserv = IN[4] # Запас для прямоугольного проёма как отношение сторон
else:
	rectnReserv = IN[4] / 304.8 # Запас для прямоугольного проёма в мм
if roundReservType:
	roundReserv = IN[5] # Запас для круглого проёма как отношение сторон
else:
	roundReserv = IN[5] / 304.8 # Запас для круглого проёма в мм
koef = IN[6] # Максимальное отношение сторон для круглого отверстия
maxDiam = IN[7] / 304.8 # Максимальный диаметр для круглого отверстия
isLink = IN[8] # Определение того, в связанном ли файле находятся сети
nameLink = IN[9] # Часть имени файла с сетями для корректного определения

# Фильтрация связанных файлов
if isLink:
	a = 0
	for inst in linkInstances:
		if nameLink in inst.Name:
			linkDoc = inst.GetLinkDocument()
			a = 1
	if a == 0:
		linkDoc = doc
else:
	linkDoc = doc

# Получение коллекции всех экземпляров стен
walls = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).WhereElementIsNotElementType().ToElements()

# Функция расчёта и создания проёма
def creation(communication):
	# Получение имени категории коммуникации
	categName = communication.Category.Name
	# Определение параметров ширины и высоты для труб
	if categName == 'Трубы':
		# Получение внешнего диаметра трубы
		commDiam = communication.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble()
		commWidth = commDiam
		commHeight = commDiam
	# Определение параметров ширины и высоты для воздуховодов	
	elif categName == 'Воздуховоды':
		# Определение формы сечения воздуховода
		sect = communication.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString()
		if sect == 'Воздуховод круглого сечения':
			commDiam = communication.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble()
			commWidth = commDiam
			commHeight = commDiam
		elif sect == 'Воздуховод овального сечения' or sect == 'Воздуховод прямоугольного сечения':
			commWidth = communication.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble()
			commHeight = communication.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble()
	# Получение коэффициентов уравнения прямой (аналогично стене)
	pipeCurve = communication.Location.Curve # Получение кривой эскиза трубы
	line = geomSolid.IntersectWithCurve(pipeCurve, optS).GetCurveSegment(0) # Получение геометрии (списка кривых) пересечения и взятие первой и единственной кривой
	end0 = line.GetEndPoint(0)
	end1 = line.GetEndPoint(1)
	x0_ = end0.X; y0_ = end0.Y;	x1_ = end1.X; y1_ = end1.Y; z0_ = end0.Z; z1_ = end1.Z
	A2 = y0_-y1_; B2 = x1_-x0_; C2 = z1_-z0_
	
	# Получение центра пересечения стены и трубы
	center = XYZ((end0.X + end1.X) / 2, (end0.Y + end1.Y) / 2, (end0.Z + end1.Z) / 2) # Получение центра пересечения стены и трубы
	
	# Получение угла пересечения кривой стены и кривой трубы в плане (для определения минимальной ширины проёма)
	cosin = (A1*A2 + B1*B2) / ((A1**2 + B1**2)**0.5 * (A2**2 + B2**2)**0.5) # Вычисление косинуса между прямыми трубы и стены
	angleHor = math.degrees(math.acos(cosin)) # Вычисление горизонтального угла (в градусах)
	if angleHor > 90:
		angleHor = 180 - angleHor # Определение острого угла при пересечении
	newAngleHor = math.radians(angleHor) # Перевод острого угла в радианы
	# Определение минимальной ширины проёма
	minWidth = width/math.tan(newAngleHor) + commWidth/math.sin(newAngleHor)
	# Определение ширины проёма с учётом допуска
	if rectnReservType:
		openWidth = minWidth * rectnReserv
	else:
		openWidth = minWidth + rectnReserv
	
	# Получение угла пересечения кривой стены и кривой трубы по вертикали (для определения минимальной высоты проёма, аналогично предыдущему)
	cosin = C2 / ((A2**2 + B2**2+C2**2)**0.5) # Вычисление косинуса между трубой и горизонтальной плоскостью
	angleVert = math.degrees(math.acos(cosin)) # Вычисление вертикального угла (в градусах)
	if angleVert > 90:
		angleVert = 180 - angleVert
	newAngleVert = math.radians(angleVert)
	# Определение минимальной высоты проёма
	minHeight = width/math.tan(newAngleVert) + commHeight/math.sin(newAngleVert)
	# Определение высоты проёма с учётом допуска
	if rectnReservType:
		openHeight = minHeight * rectnReserv
	else:
		openHeight = minHeight + rectnReserv
	
	# Округление размеров до сантиметров
	openWidth = round(openWidth * 304.8, -1) / 304.8
	openHeight = round(openHeight * 304.8, -1) / 304.8
	
	# Проверка того, нужен ли круглый проём, и создание проёма
	if openHeight > openWidth: # Случай, если высота больше ширины
		if openHeight/openWidth <= koef and openHeight <= maxDiam: # Проверка на допустимые значения
			# Создание круглого проёма и задание его диаметра
			cutNew = doc.Create.NewFamilyInstance(center, roundOpen, wall, doc.GetElement(wall.LevelId), nonStr)
			cutNew.LookupParameter('Диаметр проёма').Set(openHeight)
		else:
			# Создание прямоугольного проёма и задание его ширины и высоты
			cutNew = doc.Create.NewFamilyInstance(center, rectnOpen, wall, doc.GetElement(wall.LevelId), nonStr)
			cutNew.LookupParameter('Ширина проёма').Set(openWidth)
			cutNew.LookupParameter('Высота проёма').Set(openHeight)
	else: # Случай, если ширина больше высоты (аналогично)
		if openWidth/openHeight <= koef and openWidth <= maxDiam:
			cutNew = doc.Create.NewFamilyInstance(center, roundOpen, wall, doc.GetElement(wall.LevelId), nonStr)
			cutNew.LookupParameter('Диаметр проёма').Set(openWidth)
		else:
			cutNew = doc.Create.NewFamilyInstance(center, rectnOpen, wall, doc.GetElement(wall.LevelId), nonStr)
			cutNew.LookupParameter('Ширина проёма').Set(openWidth)
			cutNew.LookupParameter('Высота проёма').Set(openHeight)
	cutNew.LookupParameter('Дисциплина проёма').Set(categName)
			
	return communication, cutNew

# Формирование пустого выходного списка
lst = []

# Открытие транзакции
TransactionManager.Instance.EnsureInTransaction(doc)

# Аквтивация загруженных семейств проёмов (если ещё не были использованы)
rectnOpen.Activate()
roundOpen.Activate()

# Обработка списка экземпляров стен
for wall in walls:
	# Получение ширины
	width = wall.Width
	
	# Получение коэффициентов уравнения прямой
	wallCurve = wall.Location.Curve # Получение кривой эскиза стены
	endWall0 = wallCurve.GetEndPoint(0) # Получение начальной точки кривой
	endWall1 = wallCurve.GetEndPoint(1) # Получение конечной точки кривой
	x0 = endWall0.X; y0 = endWall0.Y; x1 = endWall1.X; y1 = endWall1.Y # Получение отдельных координат точек
	A1 = y0-y1;	B1 = x1-x0 # Получение коэффициентов уравнения прямой
	
	# Получение геометрии стены
	geomElem = wall.get_Geometry(opt)
	for geomObj in geomElem:
		geomSolid = geomObj
		
	# Получение коллекции всех экземпляров труб в проекте (необходимо каждый раз брать заново)
	pipes = FilteredElementCollector(linkDoc).OfCategory(BuiltInCategory.OST_PipeCurves).WhereElementIsNotElementType()
	# Извлечение только тех из них, которые пересекаются с объёмом данной стены
	pipeInters = pipes.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	
	# Получение коллекции всех экземпляров труб в проекте (необходимо каждый раз брать заново)
	ducts = FilteredElementCollector(linkDoc).OfCategory(BuiltInCategory.OST_DuctCurves).WhereElementIsNotElementType()
	# Извлечение только тех из них, которые пересекаются с объёмом данной стены
	ductInters = ducts.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	
	# Формирование пустого промежуточного списка
	_lst = []
	
	# Обработка труб, пересекающихся с данной стеной
	for pipe in pipeInters:
		result = creation(pipe)	
		_lst.append(result)
	
	# Обработка воздуховодов, пересекающихся с данной стеной	
	for duct in ducts:
		result = creation(duct)	
		_lst.append(result)
		
	lst.append(_lst)

# Закрытие транзакции
TransactionManager.Instance.TransactionTaskDone()

OUT = lst