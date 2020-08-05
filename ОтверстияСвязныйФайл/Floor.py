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
date = IN[10] # Дата или другой комментарий, указывающий на версию задания

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
	
# Получение коллекции всех экземпляров плит
floors = FilteredElementCollector(linkDoc).OfCategory(BuiltInCategory.OST_Floors).WhereElementIsNotElementType().ToElements()

# Функция расчёта и создания проёма
def creation(communication, width):
	# Получение имени категории коммуникации
	categName = communication.Category.Name
	# Определение параметров ширины и высоты для труб
	if categName == 'Трубы':
		# Получение внешнего диаметра трубы
		commDiam = communication.get_Parameter(BuiltInParameter.RBS_PIPE_OUTER_DIAMETER).AsDouble()
		commWidth = commDiam
		commHeight = commDiam
		direction = XYZ(0, 0, 0)
	# Определение параметров ширины и высоты для воздуховодов	
	elif categName == 'Воздуховоды':
		# Определение формы сечения воздуховода
		sect = communication.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM).AsValueString()
		if sect == 'Воздуховод круглого сечения':
			commDiam = communication.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM).AsDouble()
			commWidth = commDiam
			commHeight = commDiam
			direction = XYZ(0, 0, 0)
		# Если воздуховод прямоугольного или овального сечения, вместе с шириной и высотой отверстия необходимо найти угол поворота в плане
		elif sect == 'Воздуховод прямоугольного сечения':
			commWidth = communication.get_Parameter(BuiltInParameter.RBS_CURVE_WIDTH_PARAM).AsDouble()
			commHeight = communication.get_Parameter(BuiltInParameter.RBS_CURVE_HEIGHT_PARAM).AsDouble()
			commGeom = communication.get_Geometry(opt)
			# Для определения направления берём боковую сторону воздуховода и находим нормаль к ней
			for geomObj in commGeom:
				commGeomSolid = geomObj
			commFaces = commGeomSolid.Faces
			for face in commFaces:
				faceNormal = face.FaceNormal
				# Проверка того, что берётся именно боковая грань
				if faceNormal.Z == 0:
					direction = faceNormal
	# Определение параметров ширины и высоты для кабельных лотков
	elif categName == 'Короба':
		# Получение внешнего диаметра короба
		commDiam = communication.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM).AsDouble()
		commWidth = commDiam
		commHeight = commDiam
		direction = XYZ(0, 0, 0)
	# Определение параметров ширины и высоты для воздуховодов	
	elif categName == 'Кабельные лотки':
		# Вместе с шириной и высотой отверстия необходимо найти угол поворота в плане
		commWidth = communication.get_Parameter(BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM).AsDouble()
		commHeight = communication.get_Parameter(BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM).AsDouble()
		commGeom = communication.get_Geometry(opt)
		# Для определения направления берём боковую сторону кабельного лотка и находим нормаль к ней (
		for geomObj in commGeom:
			commGeomSolid = geomObj
		commFaces = commGeomSolid.Faces
		for face in commFaces:
			faceNormal = face.FaceNormal
			# Проверка того, что берётся именно боковая грань
			if faceNormal.Z == 0:
				# В отличие от воздуховода, у кабельного лотка берём не нормаль, а касательную
				direction = XYZ(faceNormal.Y, -faceNormal.X, faceNormal.Z)

	# Получение коэффициентов уравнения прямой
	commCurve = communication.Location.Curve # Получение кривой эскиза
	line = geomSolid.IntersectWithCurve(commCurve, optS).GetCurveSegment(0) # Получение геометрии (списка кривых) пересечения и взятие первой и единственной кривой
	end0 = line.GetEndPoint(0)
	end1 = line.GetEndPoint(1)
	x0_ = end0.X; y0_ = end0.Y;	x1_ = end1.X; y1_ = end1.Y; z0_ = end0.Z; z1_ = end1.Z
	A2 = y0_-y1_; B2 = x1_-x0_; C2 = z1_-z0_
	
	# Получение центра пересечения плиты и коммуникации
	center = XYZ((end0.X + end1.X) / 2, (end0.Y + end1.Y) / 2, (end0.Z + end1.Z) / 2) # Получение центра пересечения плиты и комуникации
	
	# Определение ширины проёма с учётом допуска
	if rectnReservType:
		openWidth = commWidth * rectnReserv
	else:
		openWidth = commWidth + rectnReserv
	
	# Определение высоты проёма с учётом допуска
	if rectnReservType:
		openHeight = commHeight * rectnReserv
	else:
		openHeight = commHeight + rectnReserv

	# Округление размеров до сантиметров
	openWidth = round(openWidth * 304.8, -1) / 304.8
	openHeight = round(openHeight * 304.8, -1) / 304.8
	
	# Проверка того, нужен ли круглый проём, и создание проёма
	if openHeight > openWidth: # Случай, если высота больше ширины
		if openHeight/openWidth <= koef and openHeight <= maxDiam: # Проверка на допустимые значения
			# Создание круглого проёма и задание его диаметра
			cutNew = doc.Create.NewFamilyInstance(center, roundOpen, direction, doc.GetElement(floor.LevelId), nonStr)
			cutNew.LookupParameter('Диаметр проёма').Set(openHeight)
		else:
			# Создание прямоугольного проёма и задание его ширины и высоты
			cutNew = doc.Create.NewFamilyInstance(center, rectnOpen, direction, doc.GetElement(floor.LevelId), nonStr)
			cutNew.LookupParameter('Ширина проёма').Set(openWidth)
			cutNew.LookupParameter('Высота проёма').Set(openHeight)
			cutNew.LookupParameter('Поворот X').Set(direction.X)
			cutNew.LookupParameter('Поворот Y').Set(direction.Y)
	else: # Случай, если ширина больше высоты (аналогично)
		if openWidth/openHeight <= koef and openWidth <= maxDiam:
			cutNew = doc.Create.NewFamilyInstance(center, roundOpen, direction, doc.GetElement(floor.LevelId), nonStr)
			cutNew.LookupParameter('Диаметр проёма').Set(openWidth)
		else:
			cutNew = doc.Create.NewFamilyInstance(center, rectnOpen, direction, doc.GetElement(floor.LevelId), nonStr)
			cutNew.LookupParameter('Ширина проёма').Set(openWidth)
			cutNew.LookupParameter('Высота проёма').Set(openHeight)
			cutNew.LookupParameter('Поворот X').Set(direction.X)
			cutNew.LookupParameter('Поворот Y').Set(direction.Y)
	cutNew.LookupParameter('Дисциплина проёма').Set(categName)
	cutNew.LookupParameter('Глубина проёма').Set(width)
	cutNew.LookupParameter('Дата').Set(date)
			
	return communication, cutNew

# Формирование пустого выходного списка
lst = []

# Открытие транзакции
TransactionManager.Instance.EnsureInTransaction(doc)

# Аквтивация загруженных семейств проёмов (если ещё не были использованы)
rectnOpen.Activate()
roundOpen.Activate()

# Обработка списка экземпляров плит
for floor in floors:
	# Получение ширины
	width = floor.get_Parameter(BuiltInParameter.FLOOR_ATTR_THICKNESS_PARAM).AsDouble()

	# Получение геометрии стены
	geomElem = floor.get_Geometry(opt)
	for geomObj in geomElem:
		geomSolid = geomObj
		
	# Получение коллекции всех экземпляров труб в проекте (необходимо каждый раз брать заново)
	pipes = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_PipeCurves).WhereElementIsNotElementType()
	# Извлечение только тех из них, которые пересекаются с объёмом данной плиты
	pipeInters = pipes.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	
	# Получение коллекции всех экземпляров труб в проекте (необходимо каждый раз брать заново)
	ducts = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_DuctCurves).WhereElementIsNotElementType()
	# Извлечение только тех из них, которые пересекаются с объёмом данной плиты
	ductInters = ducts.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	
	# Получение коллекции всех экземпляров коробов в проекте (необходимо каждый раз брать заново)
	conds = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Conduit).WhereElementIsNotElementType()
	# Извлечение только тех из них, которые пересекаются с объёмом данной плиты
	condInters = conds.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	
	# Получение коллекции всех экземпляров кабельных лотков в проекте (необходимо каждый раз брать заново)
	cabls = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_CableTray).WhereElementIsNotElementType()
	# Извлечение только тех из них, которые пересекаются с объёмом данной плиты
	cablInters = cabls.WherePasses(ElementIntersectsSolidFilter(geomSolid)).ToElements()
	
	# Формирование пустого промежуточного списка
	_lst = []
	
	# Обработка труб, пересекающихся с данной плитой
	for pipe in pipeInters:
		result = creation(pipe, width)	
		_lst.append(result)
	
	# Обработка воздуховодов, пересекающихся с данной плитой	
	for duct in ducts:
		result = creation(duct, width)	
		_lst.append(result)
		
	# Обработка коробов, пересекающихся с данной стеной	
	for cond in conds:
		result = creation(cond, width)	
		_lst.append(result)

	# Обработка кабельных лотков, пересекающихся с данной стеной	
	for cabl in cabls:
		result = creation(cabl, width)	
		_lst.append(result)

	lst.append(_lst)

# Закрытие транзакции
TransactionManager.Instance.TransactionTaskDone()

OUT = lst