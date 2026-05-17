# Игра Соедени 4 на двух игроков на Blazzor

<img src="mdassets/game.png"/>

# Устройство 

## Connect4Game.razor

Основная страница

## FourOrderGameStateManager.cs

Хранение сессий игр (FourOrderGameState.cs) на основе IMemoryCache

## FourOrderGameState.cs

Игра

## Описание

При ининциализации из FourOrderGameStateManager получаем свой экземпляр FourOrderGameState.
FourOrderGameState На каждый ход испускает событие OnChange. .razor подписан и меняет css шайбы.

