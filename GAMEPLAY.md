# Sendero Dorado: Ecos del Bosque — exploración 3D

Proyecto Unity 6000.6.0f1. La rama `main` conserva el mapa original en el commit `448a320`.
La entrega PA4 se desarrolla en `codex/pa4-final`, a partir de `feature/gameplay-3d`.

La escena original se conserva. La escena jugable es `Assets/Scenes/Gameplay.unity`, ya configurada como escena inicial de compilación.
El terreno original se conserva como base; la rama PA4 abre claros para santuarios, botiquines y tres pasajes de piedra.

## Cómo jugar

1. Abre este proyecto con Unity 6000.6.0f1 y la rama `codex/pa4-final`.
2. Abre `Assets/Scenes/Gameplay.unity` y pulsa Play en Unity.
3. Selecciona **Jugar**. Usa WASD o flechas para mover la esfera respecto a la cámara, y el mouse para orbitar. ESC abre y cierra la pausa y libera el cursor.
4. Busca doce reliquias de luz, activadas en etapas de 3, 4 y 5. La flecha superior señala un sector aproximado hacia la reliquia disponible más cercana; no indica posición ni distancia.
5. Descubre los tres santuarios opcionales acercándote a sus piedras antiguas. El HUD muestra cuántos lugares encontraste.
6. Cada reliquia agrega un guardián al bosque. Un contacto resta 25 de los 100 puntos de vida; los botiquines verdes curan 35. Reúne las doce reliquias antes de perder toda la vida o agotar el tiempo.

La pausa ofrece Continuar, Reiniciar partida, Volver al menú principal y Salir del juego. Salir termina Play en el editor y cierra la aplicación compilada. Perder el foco pausa la partida.
La partida es de una sesión: reiniciar o volver al menú restablece las doce reliquias, los guardianes adicionales, los botiquines, los tres hallazgos y la posición inicial. Los controles se muestran solo al pausar.

## Escena y componentes

Los nombres técnicos `Coin`, `CoinManager` y `GoldenCoin` vienen del prototipo anterior. En la aventura final estos objetos son las **reliquias de luz**; se conservan las clases para no romper referencias serializadas de Unity.

Todas las referencias indicadas ya están asignadas en el Inspector. Los scripts de juego están en `Assets/Scripts/Gameplay`, bajo el espacio de nombres `ForestJourney`.

| Script | GameObject | Función y referencias |
| --- | --- | --- |
| PlayerController | Player Sphere | Movimiento relativo a cámara, velocidad 14, aceleración 32, adaptación a pendientes y recuperación de caídas. Referencia Gameplay Camera. Rigidbody de masa 2, interpolación y detección continua; SphereCollider de radio 1. |
| BallRollingVisual | Player Sphere / Rolling Visual | Rueda la pelota según distancia recorrida, independientemente del giro horizontal del personaje. Referencia Player Sphere. |
| CameraController | Gameplay Camera | Órbita TPS con mouse, Pitch limitado y colisión; referencia Player Sphere. Distancia 7, altura de pivote 1,6 y hombro 0,55. |
| Coin | Coin 01 a Coin 12 | Trigger esférico, recolección única y desactivación. Referencia CoinManager de Gameplay Systems. |
| CoinMotion | Floating Coin Visual, dentro de cada moneda | Flotación y giro del modelo; no mueve el trigger. |
| CoinManager | Gameplay Systems | Registro de doce reliquias en etapas de 3, 4 y 5; contador, reinicio y búsqueda de la más cercana. |
| CoinCounterUI | Gameplay Interface / Exploration HUD | Texto, barra de progreso y panel de objetivo completado. Referencias a CoinManager y elementos UI. |
| CoinDirectionIndicator | Exploration HUD | Flecha hacia la moneda más cercana, cuantizada en sectores de 30°. Referencias a jugador, cámara, registro y flecha. |
| CompassArrow | Direction Guide / Compass Arrow | Dibuja la flecha dorada; incluye CanvasRenderer y no intercepta clics. |
| GameManager | Gameplay Systems | Estados de menú, juego, pausa y resultados; controla tiempo, 100 puntos de vida, daño, curación y reinicio. |
| GuardianDirector | Gameplay Systems | Empieza con un guardián y añade otro por cada reliquia cerca del siguiente objetivo, respetando distancia de aparición. |
| ForestGuardian | Forest Guardian y refuerzos | Patrulla, detección, persecución y contacto; velocidad cercana pero inferior a la del jugador. |
| Medkit | Forest Remedies | Seis botiquines que curan 35 puntos cuando falta vida y se restauran al reiniciar. |
| PauseMenu | Gameplay Systems | Entrada ESC, pausa al perder el foco y acciones de los botones. Referencia GameManager. |
| CollectionFeedback | Gameplay Systems | Sonido sintetizado y partículas doradas al recoger monedas. AudioSource, clip CoinChime y material CollectionSpark asignados. |

El jugador utiliza la capa **Ignore Raycast** para evitar que las consultas de suelo y cámara detecten su propio collider; sigue colisionando físicamente con el entorno.
La configuración TPS, sensibilidades, velocidad de giro y preparación para apuntado se detallan en [Documentation/TPS_CONTROLLER.md](Documentation/TPS_CONTROLLER.md).
Los prefabs reutilizables están en `Assets/Prefabs/Gameplay`: PlayerSphere, GoldenCoin, ForestGuardian, ForestMedkit, MoonstonePassage y AncientWaystone. Al instanciarlos en otra escena, asigna sus referencias en el Inspector.

## Conservación del mapa

- `main` mantiene intacto el mapa original. En `codex/pa4-final`, `SampleScene.unity` conserva sus objetos, pero comparte `New Terrain.asset` con Gameplay; los claros PA4 afectan ambos usos del terreno.
- Gameplay contiene una copia de los objetos de la escena original y añade objetivos, pasajes y sistemas de juego.
- Los prefabs importados de vegetación no tenían colliders. La nueva escena añade proxies de troncos y rocas bajo **Environment Collision - Terrain Instances**, y colliders a las rocas independientes, sin editar los recursos originales.
- La colocación inicial comprobó pendiente, espacio para la esfera y conexión mediante una cuadrícula de terreno. Las doce reliquias ocupan zonas diferentes; las dos adicionales se colocaron sobre terreno transitable.
- La distancia de vegetación pequeña se limita a 90 unidades y la de árboles a 450. La densidad del detalle se redujo para mejorar visibilidad y rendimiento.

## Herramientas y verificación

Las herramientas de `Assets/Editor` son exclusivas del editor. Los informes y comandos temporales se guardan en `Library`, excluido de Git.

- **Tools > Forest Journey > Verify gameplay in Play Mode** ejecuta la verificación reproducible. Inicia Play y deja el menú principal abierto antes de ejecutarla. Durante la prueba controla un teclado virtual, utiliza obstáculos temporales y termina en el menú principal.
- Comprueba botones de inicio/continuación, WASD/flechas, rodamiento, pared, rampa de 12°, pausa, doce triggers por etapas, rechazo de duplicados, cambio de objetivo, finalización, reinicio y recuperación de caídas. También detecta errores de ejecución.
- El informe se guarda en `Library/GameplayQA.txt`; las capturas de menú, juego, pausa y finalización se guardan en la misma carpeta.
- **Tools > Forest Journey > Build Windows prototype** compila en `Builds/SenderoDorado/SenderoDorado.exe`. Para compartir el ejecutable, incluye toda la carpeta SenderoDorado.
- La carpeta `SenderoDorado_BackUpThisFolder_ButDontShipItWithYourGame` contiene símbolos de respaldo y puede omitirse al distribuir el juego. El paquete final está en `output/build`.
- **Create gameplay scene** es una herramienta de creación inicial; usa directamente la escena existente para conservar ajustes manuales.

La verificación automática comprueba rutas geométricas y teletransporta al jugador entre monedas para probar todos los triggers; no sustituye una valoración humana de la comodidad de todo el recorrido.

## Shader Graph de las monedas

El material `Assets/Materials/Gameplay/Coin - Warm gold.mat` utiliza el Shader Graph propio `Assets/Shaders/Gameplay/GoldenCoinPulse.shadergraph`. El prefab `GoldenCoin` comparte ese material en su aro dorado, por lo que el efecto se observa en todas las monedas del nivel sin cambiar los triggers ni el sistema de recolección.

El grafo conserva una superficie metálica e iluminada por URP y anima la **emisión dorada** con nodos `Time`, `Multiply`, `Sine` y `Add`. Su cálculo principal es `CoinGold × (0,6 + 0,4 × sin(Time × 3)) × 2,5`: el tiempo alimenta una onda periódica, el factor 0,4 controla la amplitud, el valor 0,6 mantiene el brillo y 2,5 refuerza la emisión. En el video se puede abrir el grafo, seguir esas conexiones de izquierda a derecha y mostrar las monedas mientras se mueven en la escena.

## Git

`main` conserva exclusivamente el respaldo inicial `448a3200dbc27ff6560e1ebe7ec3ee717f9e88d1`. La rama `feature/gameplay-3d` contiene los commits progresivos del prototipo. No se hizo merge hacia main.

Las carpetas Library, Temp, Logs y Builds están excluidas. Los recursos binarios configurados en `.gitattributes` usan Git LFS; activa Git LFS antes de clonar o descargar todos los recursos del repositorio.
