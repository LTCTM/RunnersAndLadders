import pygame
pygame.init()
#显示
WHITE = 255,255,255
BACKGROUND = 0,0,127
SCREEN_WIDTH = 1000
SCREEN_HEIGHT = 1000
SCREEN = pygame.display.set_mode((SCREEN_WIDTH,SCREEN_HEIGHT))

def print_line(text,rect=None,font_size=None,full_fill=False,center=True,color=WHITE):
	"""将字填充在指定区域
text:单行字符串
rect:目标区域，为元组，格式：(left,top,width,height)。默认全屏
font_size:字体占区域高度的百分比。默认值为充满
full_fill:为True时，会将偏小的字放大充满整个区域的90%，否则只会压缩偏大的字至整个区域的90%
center:偏小的字体是否需要居中，为bool"""
	#==========获得值==========
	if not rect:
		rect = 0,0,SCREEN_WIDTH,SCREEN_HEIGHT
	section_size = round(rect[2]),round(rect[3])
	section_pos = round(rect[0]),round(rect[1])
	if font_size is None or font_size > 1:
		font_size = 1
	font = pygame.font.Font(None,round(section_size[1] * font_size))
	#==========操作==========
	#原始字体图片
	img_text = font.render(text,True,color)
	if full_fill or (img_text.get_width() > section_size[0] or img_text.get_height() > section_size[1]):
		#字体变形，刚好充满整个区域。此时居中与否没有区别，直接从左上角开始绘制
		img_text = pygame.transform.scale(img_text,section_size)
	elif center:
		#字体偏小，居中绘制
		section_pos = section_pos[0] + (section_size[0] - img_text.get_width()) / 2,section_pos[1] + (section_size[1] - img_text.get_height()) / 2
	SCREEN.blit(img_text,section_pos)
def print_text(text,rect=None,font_size=None,full_fill=False,center=True,color=WHITE):
	"""将字填充在指定区域
text:可以是字符串或字符串元组。每个字符串可以包含多行
rect:目标区域，为元组，格式：(x,y,width,height)。默认全屏
font_size:字体占区域高度的百分比。默认值为充满
full_fill:为True时，会将偏小的字放大充满整个区域的90%，否则只会压缩偏大的字至整个区域的90%
center:偏小的字体是否需要居中，为bool"""
	#==========默认值==========
	if not rect:
		rect = 0,0,SCREEN_WIDTH,SCREEN_HEIGHT
	#分割多行字符
	if isinstance(text,str):
		line_texts = text.split("\n")
	else:
		line_texts = []
		for ele in text:
			line_texts.extend(ele.split("\n"))
	line_count = len(line_texts)
	if font_size:
		section_height = font_size * rect[3]
		section_top = rect[1] + (rect[3] - section_height * line_count) / 2
		if section_height * line_count > rect[3]:
			section_height = rect[3] / line_count
			section_top = 0
	else:
		section_height = rect[3] / line_count
		section_top = 0

	for i in range(0,line_count):
		line_rect = rect[0],section_top + i * section_height,rect[2],section_height
		print_line(line_texts[i],line_rect,1,full_fill,center,color)
#Enum
from enum import Enum
#方向
class Direction(Enum):
	left = 1
	up = 2
	right = 3
	down = 4
#格子类型
class GridType(Enum):
	air = 1
	gold = 2
	ladder = 3
	stick = 4
	brick = 5
	stone = 6
	trap = 7
#结构
class Position(object):
	def __init__(self,grid_size,*args):
		"args:一个参数：元组或Position对象，两个参数：x,y"
		self.__grid_size = grid_size
		if len(args) == 1:
			self.x = args[0][0]
			self.y = args[0][1]
		else:
			self.x = args[0]
			self.y = args[1]
	def __call__(self):
		return self.x,self.y
	def __getitem__(self,index):
		return self.x if index == 0 else self.y
	def __eq__(self, other):
		return self.x == other.x and self.y == other.y
	def int_tuple(self):
		return int(self.x),int(self.y)
	def logic_to_screen(self):
		"返回自身对应的屏幕坐标，为Position对象"
		return Position(self.__grid_size,self.x * self.__grid_size,self.y * self.__grid_size)
	def screen_to_logic(self):
		"返回自身对应的逻辑坐标，为Position对象"
		return Position(self.__grid_size,self.x / self.__grid_size,self.y / self.__grid_size)
	def copy(self):
		"克隆自身"
		return Position(self.__grid_size,self)
#函数
def color_to_tuple(color):
	return color.r,color.g,color.b
def processed_img(file_name,new_size,color_key=None):
	"file_name:全路径"
	img = pygame.image.load(file_name)
	img = img.convert()
	img = pygame.transform.scale(img,(new_size,new_size))
	if color_key:
		img.set_colorkey(color_key)
	return img