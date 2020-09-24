from head import *
from pygame.locals import *
import os
import time
from task import Task
from character import Runner,RedOne
from field import Field

class Game(object):
	MAX_CAUGHT_TIME = 1
	TRAP_RECOVER_TIME = 4
	def __init__(self,map_index):
		"map_index：地图编号"
		#系统
		self.__pressed_keys = pygame.key.get_pressed()
		self.__start_time = time.clock()
		#游戏内容
		self.result = None
		self.__runner_caught_count = 0
		self.__gold_got = 0
		self.field = Field(map_index,self)
		self.runner = Runner(self,self.field.runner_init_location)
		self.red_ones = []
		for ROIL in self.field.red_ones_init_location:
			self.red_ones.append(RedOne(self,ROIL))
		self.__all_characters = tuple(self.red_ones) + (self.runner,)
		self.__traps = []
	def dig_trap(self,location,create_time):
		"location:为元组或Position对象，create_time:创造陷阱的主时间main_time"
		self.__traps.append((location,create_time))
		self.field.set_grid(location,GridType.trap)
	@property
	def living_red_ones_location(self):
		return (red_one.location for red_one in self.red_ones if not red_one.dead)
	def __listen_input(self):
		"消息处理"
		while not self.result:
			for event in pygame.event.get():
				if event.type == QUIT:
					os._exit(0)
			self.__pressed_keys = pygame.key.get_pressed()
			if self.__pressed_keys[K_ESCAPE]:
				self.result = "Cancel"
	def __heart_process(self):
		"游戏过程（心跳过程）"
		runner_last_die_time = 0
		while not self.result:
			main_time = time.clock() - self.__start_time
			self.field.draw(SCREEN)
			if self.runner.dead:
				#Runner死亡状态，一切游戏内容暂停，不显示红人，显示Runner死亡图标。到一定时间玩家复活
				self.runner.draw(SCREEN)
				self.runner.try_reborn(main_time)
			else:
				#正常游戏状态
				#陷阱可能重置
				if len(self.__traps) > 0:
					oldest_trap = self.__traps[0]
					trap_location = oldest_trap[0]
					trap_set_time = oldest_trap[1]
					if main_time >= trap_set_time + self.TRAP_RECOVER_TIME:
						self.field.set_grid(trap_location,GridType.brick)
						self.__traps.pop(0)
						#重置的陷阱可能会击杀人物
						for character in self.__all_characters:
							if character.location == trap_location:
								character.die(main_time)
				#Runner和红人接受指令或尝试复活
				for character in self.__all_characters:
					if character.dead:
						character.try_reborn(main_time)
					elif character.busy:
						character.act(main_time)
					else:
						character.get_command(main_time,self.__pressed_keys)
					character.draw(SCREEN)
				if self.runner.location in self.living_red_ones_location:
					#被抓住
					self.runner.die(main_time)
				if self.field.get_grid(self.runner.location) == GridType.gold:
					#吃到金子
					self.field.set_grid(self.runner.location,GridType.air)
					self.__gold_got+=1
					if self.__gold_got == self.field.gold_count:
						#胜利
						self.result = "You Win"
				if self.runner.dead:
					#Runner因为各种原因死亡
					runner_last_die_time = main_time
					self.__runner_caught_count+=1
					if self.__runner_caught_count > self.MAX_CAUGHT_TIME:
						#失败
						self.result = "You lose"
					#部分游戏内容重置
					for trap in self.__traps:
						self.field.set_grid(trap[0],GridType.brick)
					for red_one in self.red_ones:
						red_one.move_init_location()
					self.__traps.clear()
			#显示
			pygame.display.update()
		#游戏结束
		if self.result == "Cancel":
			pass
		else:
			self.field.draw(SCREEN)
			if self.runner.dead:
				self.runner.draw(SCREEN)
			else:
				for character in self.__all_characters:
					character.draw(SCREEN)
			print_text(self.result)
			pygame.display.update()
			time.sleep(3)
	def begin(self):
		self.__main_task = Task.start_new(self.__heart_process)
		self.__listen_input()
		self.__main_task.wait()
		return self.result

