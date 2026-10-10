# Producto Académico 4 - Sendero Dorado

## Juego y objetivo

Sendero Dorado es una partida de exploración 3D en un bosque. La esfera debe reunir las diez monedas antes de que pasen siete minutos. Hay tres oportunidades: el guardián del bosque patrulla una ruta y persigue al jugador cuando lo detecta. Al tocarlo, quita una oportunidad, devuelve la esfera al inicio y aplica una breve protección para evitar pérdidas consecutivas. Se gana al recoger las diez monedas; se pierde al agotar las oportunidades o el tiempo. Ambas condiciones muestran una pantalla final con opciones para reiniciar o volver al menú.

## Abrir y jugar

1. Abrir el proyecto con Unity **6000.6.0f1** y la rama `codex/pa4-final`.
2. Abrir `Assets/Scenes/Gameplay.unity`, pulsar **Play** y seleccionar **Jugar**.
3. Usar **WASD** o las flechas para mover la esfera en relación con la cámara. Mover el mouse para mirar alrededor. La flecha superior indica aproximadamente la dirección de la moneda más cercana.
4. Pulsar **Escape** para pausar, continuar, reiniciar o regresar al menú. La pausa libera el cursor.

## Sistemas y decisiones técnicas

- `PlayerController` conserva el Rigidbody, la detección de suelo, la aceleración, la respuesta a pendientes y la rotación hacia la marcha.
- `CameraController` sigue a la esfera, permite una órbita independiente con el mouse, limita el Pitch y se retrae cuando una pared obstruye la vista.
- `Coin` y `CoinManager` registran una sola recolección por Trigger, actualizan el objetivo y restauran las diez monedas al reiniciar.
- `GameManager` controla menú, juego, pausa, victoria y derrota; administra siete minutos, tres oportunidades y el contacto con el guardián.
- `ForestGuardian` usa dos estados: **Patrol**, entre cuatro waypoints, y **Chase**, cuando el jugador está cerca y visible. Usa Raycast para línea de visión y SphereCast para esquivar obstáculos cercanos. Su percepción se calcula cada 0,2 segundos.
- `ExpeditionHUD` muestra tiempo, oportunidades y una alerta cuando el guardián persigue al jugador. El menú de resultados sustituye al HUD al terminar.
- `CollectionFeedback` reproduce un sonido por moneda y reutiliza un grupo fijo de cuatro sistemas de partículas. Esta **técnica de optimización (Object Pooling)** evita crear y destruir un objeto de partículas en cada recolección. Los efectos usan tiempo no escalado para que el último se vea incluso al congelarse la partida.
- `Forest Ambience` reproduce en bucle una pista de viento y pájaros sintetizada para el proyecto. La escena conserva materiales diferenciados, URP, Volume global y relieve de Terrain. Las monedas emplean el Shader Graph animado propio `GoldenCoinPulse.shadergraph`.
- `PlayerSphere`, `GoldenCoin` y `ForestGuardian` son Prefabs. El controlador del guardián, el HUD y la lógica de partida tienen responsabilidades separadas.

## Pruebas y compilación

En Unity, abrir la escena, entrar en Play y ejecutar desde el menú **Tools > Forest Journey**:

- **Verify gameplay in Play Mode**: 41 comprobaciones de movimiento, colisiones, monedas, menú, pausa, victoria y reinicio.
- **Verify TPS in Play Mode**: 43 comprobaciones de cámara, WASD relativo, giro, cursor y estabilidad.
- **Verify PA4 in Play Mode**: 29 comprobaciones de patrulla, persecución, contacto, temporizador, resultados, interfaz, audio y reutilización de partículas.

Los tres informes se guardan en `Library/GameplayQA.txt`, `Library/TPSVerification.txt` y `Library/PA4QA.txt`. Sus capturas también aparecen en `Library`. Estas pruebas automáticas incluyen teletransporte entre monedas para comprobar los diez Triggers; conviene realizar además un recorrido manual completo antes de presentar el video.

**Tools > Forest Journey > Build Windows prototype** valida la escena y genera `Builds/SenderoDorado/SenderoDorado.exe`. La compilación del 10 de octubre de 2026 terminó con cero errores y 14 advertencias de recursos importados, y se verificó que el ejecutable arranca fuera del editor. Para compartirlo, conservar la carpeta `SenderoDorado_Data` junto al `.exe`. `Builds` y `Library` quedan fuera de Git.

## Entrega PA4

El informe PDF de 4 a 6 páginas está en `output/pdf/PA4_Informe_Sendero_Dorado.pdf`. El repositorio debe compartirse desde la rama `codex/pa4-final` o integrarse después en la rama de entrega acordada por el equipo. La visibilidad pública de GitHub debe confirmarse antes del envío.

La consigna solicita un video de **5 a 7 minutos** que enseñe gameplay, IA, UI, audio, efectos y la optimización. Una estructura sugerida es: 1 minuto de introducción y controles; 2 minutos de exploración y monedas; 1 minuto de patrulla/persecución; 1 minuto de victoria/derrota e interfaz; 1 minuto para explicar el pool de partículas, pruebas y Build. Todos los integrantes deben poder explicar las decisiones principales; los nombres y roles los debe añadir el equipo.
