from head import *
class Field(object):
	"游戏场地"
	#Runner和红人初始信息
	DATA_RUNNER_COLOR = 0, 0, 0
	DATA_RED_ONE_COLOR = 255, 0, 64
	def __init__(self,map_name,link_game):
		#初始值
		self.gold_count = 0
		self.__game = link_game
		#尺寸数据
		data = pygame.image.load(r"Maps\%s.bmp" % (map_name,)).convert()
		self.width = data.get_width()
		self.height = data.get_height()
		max_grid_width = int(SCREEN_WIDTH / self.width)
		max_grid_height = int(SCREEN_HEIGHT / self.height)
		self.unit_size = max_grid_width if max_grid_width < max_grid_height else max_grid_height
		#grids装的是GridType
		self.grids = [GridType.air] * (self.width * self.height)
		self.img = pygame.Surface((self.unit_size * self.width, self.unit_size * self.height))
		#数据文件的颜色对应的元素，键为颜色，值为GridType
		self.color_to_grid = {}
		self.color_to_grid[(255,255,255)] = GridType.air #白
		self.color_to_grid[(255,201,14)] = GridType.stick	#橘黄
		self.color_to_grid[(127,127,127)] = GridType.stone	#灰
		self.color_to_grid[(0,162,232)] = GridType.ladder	#青蓝
		self.color_to_grid[(34,177,76)] = GridType.gold	#绿
		self.color_to_grid[(163,61,39)] = GridType.brick	#砖色
		#元素对应的图元，键为GridType，值为Surface
		self.grid_to_element = {}
		for element in GridType.__members__.values():
			self.grid_to_element[element] = processed_img(r"Elements\%s.bmp" % (element.name,),self.unit_size)
		#绘制底板
		self.red_ones_init_location = []
		for i in range(0,self.width):
			for j in range(0,self.height):
				grid_data = data.get_at((i, j))
				if grid_data == self.DATA_RUNNER_COLOR:
					#Runner初始坐标
					self.runner_init_location = i,j
					self.set_grid((i, j), GridType.air)
				elif grid_data == self.DATA_RED_ONE_COLOR:
					self.red_ones_init_location.append((i,j))
					self.set_grid((i, j), GridType.air)
				else:
					self.set_grid((i, j), self.color_to_grid[color_to_tuple(grid_data)])
					if self.get_grid((i, j)) == GridType.gold:
						self.gold_count+=1
				self.update_grid_img((i,j))
	@property
	def red_one_count(self):
		return len(self.red_ones_init_location)
	#对下列函数，想象 x=location[0] y=location[1]
	def get_grid(self,location):
		"location:为元组或Position对象"
		return self.grids[location[0] + location[1] * self.width]
	def set_grid(self,location,value):
		"location:为元组或Position对象"
		self.grids[location[0] + location[1] * self.width] = value
		self.update_grid_img(location)
	def update_grid_img(self,location):
		"location:为元组或Position对象"
		self.img.blit(self.grid_to_element[self.get_grid(location)],(location[0] * self.unit_size,location[1] * self.unit_size))
	def draw(self,screen):
		screen.blit(self.img,(0,0))