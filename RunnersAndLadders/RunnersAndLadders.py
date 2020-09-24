from head import *
from pygame.locals import *
import os
from game import Game

pygame.display.set_caption("RunnersAndLadders")

level_count = 10
rect = 0,0,SCREEN_WIDTH,SCREEN_HEIGHT

text = []
text.append("click on level to begin game")
for i in range(1,level_count + 1):
	text.append(str(i))
line_count=len(text)
init_level_line = line_count - level_count
line_height = rect[3] / line_count
while True:
	SCREEN.fill(BACKGROUND)
	print_text(text)
	for event in pygame.event.get():
		if event.type == QUIT:
			os._exit(0)
		elif event.type == pygame.MOUSEBUTTONDOWN:
			y = event.pos[1]
			game_num = int(y // line_height - init_level_line + 1)
			try:
				game = Game(game_num)
				game.begin()
			except:
				pass
	pygame.display.update()

