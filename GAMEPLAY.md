# Exploración 3D

Proyecto Unity 6000.6.0f1. La rama `main` conserva el mapa original en el commit `448a320`.
Todo el prototipo se desarrolla en `feature/gameplay-3d`, sin merge automático.

La escena original se conserva. La escena jugable es `Assets/Scenes/Gameplay.unity`, ya configurada como escena inicial de compilación.
El terreno original se referencia sin modificar sus datos.

## Cómo jugar

1. Abre este proyecto con Unity 6000.6.0f1 y la rama `feature/gameplay-3d`.
2. Abre `Assets/Scenes/Gameplay.unity` y pulsa Play en Unity.
3. Selecciona **Jugar**. Usa WASD o flechas para mover la esfera; ESC abre y cierra la pausa.
4. Busca diez monedas. La flecha superior señala un sector aproximado hacia la moneda disponible más cercana; no indica posición ni distancia.
5. Al recoger las diez aparece el mensaje de objetivo completado. Puedes seguir explorando o reiniciar desde la pausa.

La pausa ofrece Continuar, Reiniciar partida, Volver al menú principal y Salir del juego. Salir termina Play en el editor y cierra la aplicación compilada. Perder el foco pausa la partida.
La partida es de una sesión: reiniciar o volver al menú restablece las diez monedas y la posición inicial.

## Escena y componentes

Todas las referencias indicadas ya están asignadas en el Inspector. Los scripts de juego están en `Assets/Scripts/Gameplay`, bajo el espacio de nombres `ForestJourney`.

| Script | GameObject | Función y referencias |
| --- | --- | --- |
| PlayerController | Player Sphere | Movimiento relativo a cámara, velocidad 14, aceleración 32, adaptación a pendientes y recuperación de caídas. Referencia Gameplay Camera. Rigidbody de masa 2, interpolación y detección continua; SphereCollider de radio 1. |
| BallRollingVisual | Player Sphere / Rolling Visual | Rueda la parte visual según distancia recorrida, sin rotar los ejes de control. Referencia Player Sphere. |
| CameraController | Gameplay Camera | Seguimiento suave con comprobaciones de colisión; referencia Player Sphere. Distancia 10, altura 6. |
| Coin | Coin 01 a Coin 10 | Trigger esférico, recolección única y desactivación. Referencia CoinManager de Gameplay Systems. |
| CoinMotion | Floating Coin Visual, dentro de cada moneda | Flotación y giro del modelo; no mueve el trigger. |
| CoinManager | Gameplay Systems | Registro de diez monedas, contador, reinicio y búsqueda de la más cercana. Array de monedas asignado. |
| CoinCounterUI | Gameplay Interface / Exploration HUD | Texto, barra de progreso y panel de objetivo completado. Referencias a CoinManager y elementos UI. |
| CoinDirectionIndicator | Exploration HUD | Flecha hacia la moneda más cercana, cuantizada en sectores de 30°. Referencias a jugador, cámara, registro y flecha. |
| CompassArrow | Direction Guide / Compass Arrow | Dibuja la flecha dorada; incluye CanvasRenderer y no intercepta clics. |
| GameManager | Gameplay Systems | Estados de menú, juego y pausa; controla tiempo, cursor, paneles y restauración de partida. |
| PauseMenu | Gameplay Systems | Entrada ESC, pausa al perder el foco y acciones de los botones. Referencia GameManager. |
| CollectionFeedback | Gameplay Systems | Sonido sintetizado y partículas doradas al recoger monedas. AudioSource, clip CoinChime y material CollectionSpark asignados. |

El jugador utiliza la capa **Ignore Raycast** para evitar que las consultas de suelo y cámara detecten su propio collider; sigue colisionando físicamente con el entorno.
Los prefabs reutilizables están en `Assets/Prefabs/Gameplay`: PlayerSphere y GoldenCoin. Al instanciarlos en otra escena, asigna la cámara al jugador y el CoinManager a las monedas.

## Conservación del mapa

- `Assets/Scenes/SampleScene.unity` y `Assets/New Terrain.asset` permanecen idénticos a `main`.
- Gameplay contiene una copia de los objetos de la escena original y referencia los mismos datos de terreno.
- Los prefabs importados de vegetación no tenían colliders. La nueva escena añade proxies de troncos y rocas bajo **Environment Collision - Terrain Instances**, y colliders a las rocas independientes, sin editar los recursos originales.
- La colocación comprueba pendiente, espacio para la esfera y conexión mediante una cuadrícula de terreno. Las diez monedas ocupan zonas diferentes de la región conectada próxima al inicio.
- La distancia de vegetación pequeña se limita a 90 unidades y la de árboles a 450 en Gameplay para reducir el coste gráfico. Los datos y la distribución originales no cambian.

## Herramientas y verificación

Las herramientas de `Assets/Editor` son exclusivas del editor. Los informes y comandos temporales se guardan en `Library`, excluido de Git.

- **Tools > Forest Journey > Verify gameplay in Play Mode** ejecuta la verificación reproducible. Inicia Play y deja el menú principal abierto antes de ejecutarla. Durante la prueba controla un teclado virtual, utiliza obstáculos temporales y termina en el menú principal.
- Comprueba botones de inicio/continuación, WASD/flechas, rodamiento, pared, rampa de 12°, pausa, diez triggers, rechazo de recolección duplicada, cambio de objetivo, finalización, reinicio y recuperación de caídas. También detecta errores y excepciones de ejecución.
- El informe se guarda en `Library/GameplayQA.txt`; las capturas de menú, juego, pausa y finalización se guardan en la misma carpeta.
- **Tools > Forest Journey > Build Windows prototype** compila en `Builds/SenderoDorado/SenderoDorado.exe`. Para compartir el ejecutable, incluye toda la carpeta SenderoDorado.
- **Create gameplay scene** es una herramienta de creación inicial; usa directamente la escena existente para conservar ajustes manuales.

La verificación automática comprueba rutas geométricas y teletransporta al jugador entre monedas para probar todos los triggers; no sustituye una valoración humana de la comodidad de todo el recorrido.

## Git

`main` conserva exclusivamente el respaldo inicial `448a3200dbc27ff6560e1ebe7ec3ee717f9e88d1`. La rama `feature/gameplay-3d` contiene los commits progresivos del prototipo. No se hizo merge hacia main.

Las carpetas Library, Temp, Logs y Builds están excluidas. Los recursos binarios configurados en `.gitattributes` usan Git LFS; activa Git LFS antes de clonar o descargar todos los recursos del repositorio.
