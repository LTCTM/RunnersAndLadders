from head import *
from pygame.locals import *
from time import sleep
class Character(object):
	"""抽象类。
要求子类拥有常量：
SPEED
REBORN_TIME
要求子类实现函数：
_valid_course(self,target_location)
_falling(self)为property
get_command(self,main_time,keys)
act(self,main_time)
die(main_time)
try_reborn(main_time)"""
	def __init__(self,link_game,init_location,sub_class_name):
		self._game = link_game
		self._images = {}
		self._images[Direction.left] = self._get_imgs(r"%s_Left" % (sub_class_name,))
		self._images[Direction.right] = self._get_imgs(r"%s_Right" % (sub_class_name,))
		self._images[Direction.up] = self._get_imgs(r"%s_Up" % (sub_class_name,))
		self._images[Direction.down] = self._get_imgs(r"%s_Down" % (sub_class_name,))
		self._images["dead"] = self._get_imgs(r"%s_Dead" % (sub_class_name,))
		self._dead = False
		self._order = None
		unit_size = self._game.field.unit_size
		self._actual_speed = self.SPEED * unit_size
		self.__init_location = Position(unit_size,init_location)
		self.move_init_location()
	def _get_imgs(self,file_name):
		"file_name:默认无需后缀名"
		return processed_img(r"Characters\%s.bmp" % (file_name,),self._game.field.unit_size,(255,255,255))
	@property
	def dead(self):
		return self._dead
	@property
	def busy(self):
		return self._order != None
	@property
	def _definitely_not_falling(self):
		"若绝对不会下落，返回True，否则False"
		if self.location.y >= self._game.field.height - 1:
			return True
		down_grid = self.location.copy()
		down_grid.y+=1
		my_grid_content = self._game.field.get_grid(self.location)
		down_grid_content = self._game.field.get_grid(down_grid)
		if down_grid_content in (GridType.brick,GridType.stone,GridType.ladder):
			return True
		#红人在自身下方的陷阱里
		if down_grid_content == GridType.trap and down_grid in self._game.living_red_ones_location:
			return True
		if my_grid_content in (GridType.stick,GridType.ladder):
			return True
		return False
	def move_init_location(self):
		self.location = self.__init_location.copy()
		self.destination = self.__init_location.copy()
		self._direction = Direction.down
		self._screen_location = self.destination.logic_to_screen()
		if self._try_set_course(0):
			self._order = "move"
	def _update_img(self):
		self._image = self._images[self._direction]
	def _try_set_course(self,main_time):
		"返回是否能够出发"
		#修正边缘目标
		height_restrain = self._game.field.height - 1
		width_restrain = self._game.field.width - 1
		target_location = self.location.copy()
		if self._direction == Direction.left:
			target_location.x = self.location.x - 1 if target_location.x > 0 else 0
		elif self._direction == Direction.up:
			target_location.y = self.location.y - 1 if target_location.y > 0 else 0
		elif self._direction == Direction.down:
			target_location.y = self.location.y + 1 if target_location.y < height_restrain else height_restrain
		elif self._direction == Direction.right:
			target_location.x = self.location.x + 1 if target_location.x < width_restrain else width_restrain
		self._update_img()
		#目标地点可能不合法
		if self._valid_course(target_location):
			#可以出发
			self.destination = target_location.copy()
			self.__last_move_time = main_time
			return True
		else:
			return False
	def _move(self,main_time):
		if self.__last_move_time < main_time + 0.1:
			
			#算出x方向在main_time-self.__last_move_time时间内的位移
			x_direction = -1 if self._direction == Direction.left else (1 if self._direction == Direction.right else 0)
			delta_x_distance = x_direction * self._actual_speed * (main_time - self.__last_move_time)
			self._screen_location.x = self._screen_location.x + delta_x_distance
			#算出y方向在main_time-self.__last_move_time时间内的位移
			y_direction = -1 if self._direction == Direction.up else (1 if self._direction == Direction.down else 0)
			delta_y_distance = y_direction * self._actual_speed * (main_time - self.__last_move_time)
			self._screen_location.y = self._screen_location.y + delta_y_distance
			destination_screen_position = self.destination.logic_to_screen()
			x_reach = (self._screen_location.x - destination_screen_position.x) * x_direction >= 0
			y_reach = (self._screen_location.y - destination_screen_position.y) * y_direction >= 0
			if x_reach and y_reach:
				#到达，修正坐标
				self._screen_location = destination_screen_position
				self.location = self.destination.copy()
				#可能下坠
				if self._falling:
					if self._direction != Direction.down:
						self._direction = Direction.down
						self._update_img()
					self.destination.y = self.location.y + 1
				else:
					self._order = None
			self.__last_move_time = main_time
	def die(self,main_time):
		if not self._dead:
			self._image = self._images["dead"]
			self._dead = True
			self._die_time = main_time
	def try_reborn(self,main_time):
		if self._dead and main_time >= self._die_time + self.REBORN_TIME:
			self._dead = False
			self.move_init_location()
	def draw(self,screen):
		screen.blit(self._image,self._screen_location.int_tuple())

class Runner(Character):
	"玩家"
	SPEED = 2				#速度，格/秒
	REBORN_TIME = 2          #重生时间，秒
	def __init__(self,parent_game,init_location):
		"parent.game.field需要先行创建"
		Character.__init__(self,parent_game,init_location,"Player")		
		self.caught_time = 0
	def _valid_course(self,target_location):
		"target_location:为Position对象。潜在的目的地，可能无法到达"
		my_grid_content = self._game.field.get_grid(self.location)
		target_grid_content = self._game.field.get_grid(target_location)
		if target_grid_content in (GridType.brick,GridType.stone):
			return False
		if self._direction == Direction.up and not my_grid_content == GridType.ladder:
			return False
		return True
	@property
	def _falling(self):
		"返回是否需要下坠"
		return not self._definitely_not_falling
	def get_command(self,main_time,keys):
		if keys[K_w] or keys[K_a] or keys[K_s] or keys[K_d]:
			if keys[K_a]:
				self._direction = Direction.left
			elif keys[K_w]:
				self._direction = Direction.up
			elif keys[K_s]:
				self._direction = Direction.down
			elif keys[K_d]:
				self._direction = Direction.right
			if self._try_set_course(main_time):
				self._order = "move"
		elif keys[K_j]:
			self._order = "dig,left"
		elif keys[K_k]:
			self._order = "dig,right"
	def act(self,main_time):
		if self._order == "move":
			self._move(main_time)
		elif self._order.split(',')[0] == "dig":
			self.__dig(main_time,self._order.split(',')[1])
	def __dig(self,main_time,dig_direction):
		down_OK = self.location.y < self._game.field.height - 1
		left_OK = dig_direction == "right" or self.location.x > 0
		right_OK = dig_direction == "left" or self.location.x < self._game.field.width - 1
		if down_OK and left_OK and right_OK:
			above_target = self.location.copy()
			above_target.x+= (1 if dig_direction == "right" else -1)
			if self._game.field.get_grid(above_target) in (GridType.air,GridType.gold,GridType.trap):
				target_grid = above_target.copy()
				target_grid.y +=1
				if self._game.field.get_grid(target_grid) == GridType.brick:
					self._game.dig_trap(target_grid,main_time)
		self._order = None

class RedOne(Character):
	"红人"
	SPEED = 1.4 #格/秒
	REBORN_TIME = 2 #重生时间，秒
	def __init__(self,parent_game,init_location):
		"parent.game.field需要先行创建"
		Character.__init__(self,parent_game,init_location,"RedOne")		
	def _valid_course(self,target_location):
		"target_location:为Position对象。潜在的目的地，可能无法到达"
		my_grid_content = self._game.field.get_grid(self.location)
		target_grid_content = self._game.field.get_grid(target_location)
		if target_grid_content in (GridType.brick,GridType.stone):
			return False
		if self._direction == Direction.up and not my_grid_content in (GridType.ladder,GridType.trap):
			return False
		if my_grid_content == GridType.trap and self._direction in (Direction.left,Direction.right):
			return False
		return True
	@property
	def _falling(self):
		"返回是否需要下坠"
		if self._definitely_not_falling:
			return False
		my_grid_content = self._game.field.get_grid(self.location)
		down_grid_content = self._game.field.get_grid((self.location.x,self.location.y + 1))
		if my_grid_content == GridType.trap and down_grid_content != GridType.trap:
			return False
		return True
	def get_command(self,main_time,keys):
		"红人的行动，可以通过参数keys了解玩家的打算"
		runner = self._game.runner
		self._direction = Direction.left if runner.location.x < self.location.x else Direction.right
		#self._direction = Direction.down
		if self._try_set_course(main_time):
			self._order = "move"
	def act(self,main_time):
		if self._order == "move":
			self._move(main_time)
