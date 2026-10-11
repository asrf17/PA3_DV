# Producto Académico 4 — Sendero Dorado: Ecos del Bosque

## Juego y objetivo

Sendero Dorado es una aventura breve de exploración y supervivencia 3D. La esfera viajera recupera doce reliquias de luz en tres etapas: 3, 4 y 5. También puede descubrir tres santuarios opcionales: Santuario del Alba, Mirador del Viento y Ruinas del Eco. El bosque comienza con dos guardianes; cada reliquia agrega otro cerca del siguiente objetivo y los cambios de etapa suman refuerzos. Hay siete minutos y 100 puntos de vida; cada contacto resta 25 y da protección breve, sin devolver al jugador al inicio. Seis botiquines recuperan 35 puntos cada uno. Cinco cristales celestes aumentan la velocidad un 45 % durante ocho segundos. El jugador alcanza mayor velocidad que los guardianes y puede evadirlos usando distancia y obstáculos. La vida a cero o el tiempo agotado causan derrota; reunir las doce reliquias produce victoria. Ambas pantallas permiten reiniciar o volver al menú.

La inspiración es la sensación de exploración de aventuras fantásticas de mundo abierto. Personajes, modelos sencillos, historia, nombres y recursos son propios de este proyecto; no se usan personajes ni arte de otra franquicia.

## Abrir y jugar

1. Abre el proyecto con Unity **6000.6.0f1** y la rama `pa4-final`.
2. Abre `Assets/Scenes/Gameplay.unity`, pulsa **Play** y selecciona **Jugar**.
3. Usa **WASD** o las flechas para mover la esfera respecto a la cámara, **Espacio** para saltar y el mouse para mirar alrededor. La flecha superior señala aproximadamente la reliquia pendiente más cercana.
4. Entra en los círculos de las piedras antiguas para descubrir los santuarios. Son opcionales y se reinician al comenzar otra expedición.
5. Pulsa **Escape** para pausar. La pausa libera el cursor y detiene al personaje, el tiempo y la IA.
6. Recoge botiquines verdes cuando falte vida y cristales celestes para acelerar. Los controles se muestran en la pausa; durante la partida la barra de vida ocupa la zona inferior.

## Sistemas y decisiones técnicas

- `PlayerController` conserva el Rigidbody, la detección de suelo, aceleración, salto, respuesta a pendientes, impulso de velocidad y giro hacia la marcha.
- `CameraController` sigue a la esfera con órbita independiente, limita el Pitch y se acerca cuando encuentra obstáculos.
- `Coin` y `CoinManager` mantienen los Triggers originales. Doce reliquias se activan por etapas de 3, 4 y 5; cada una se recoge una sola vez.
- `ExplorationLandmark` y `DiscoveryJournal` añaden tres lugares opcionales. Sus Triggers registran el hallazgo solo una vez; el contador y nombre aparecen en `ExpeditionHUD`. El registro se restablece en una nueva partida.
- `GameManager` controla menú, juego, pausa, victoria, derrota, tiempo, vida, daño, curación y reinicios.
- `GuardianDirector` inicia con dos guardianes, agrega otro por reliquia y suma refuerzos al cambiar de etapa, a distancia segura del jugador. `ForestGuardian` alterna entre **Patrol** y **Chase**; usa Raycast para comprobar visión y SphereCast para evitar obstáculos. Actualiza la percepción cada 0,2 segundos. La velocidad de persecución es inferior a la del jugador, pero suficientemente cercana para crear presión.
- `Medkit` cura 35 puntos si falta vida, desaparece al usarse y se restaura al reiniciar. `SpeedBoostPickup` activa un impulso temporal y también se restaura al reiniciar.
- `CollectionFeedback` reproduce una campanada por reliquia y reutiliza cuatro sistemas de partículas. Este **Object Pooling** evita crear y destruir partículas en cada recogida. Usa tiempo no escalado para conservar el último efecto al ganar.
- `Forest Ambience` reproduce viento y pájaros sintetizados. El Terrain conserva relieve y vegetación; se despejaron claros para tres pasajes de piedra y botiquines. Las reliquias combinan cristales luminosos y un Shader Graph propio con pulso de emisión. La interfaz usa paneles translúcidos de esquinas redondeadas, barra de vida inferior, señal direccional de daño y controles solo en la pausa.
- `PlayerSphere`, `GoldenCoin`, `ForestGuardian`, `ForestMedkit`, `MoonstonePassage` y `AncientWaystone` son Prefabs reutilizables.

## Pruebas y compilación

En Play Mode, el menú **Tools > Forest Journey** ejecuta las suites:

- **Verify gameplay in Play Mode**: 45 comprobaciones de movimiento, colisiones, Triggers, menú, pausa, victoria y reinicio.
- **Verify TPS in Play Mode**: 46 comprobaciones de cámara, WASD relativo, giro, cursor y estabilidad.
- **Verify PA4 in Play Mode**: 69 comprobaciones de patrulla, persecución, refuerzos, etapas, daño, curación, tiempo, resultados, UI, audio, pool y santuarios.

Los informes y capturas se guardan localmente en `Library`. Las 160 comprobaciones registradas incluyen traslados directos para cubrir todos los Triggers; conviene recorrer también el bosque manualmente para valorar la experiencia. La validación previa al Build comprueba referencias, Triggers, santuarios, HUD, botiquines y ausencia de scripts faltantes.

**Tools > Forest Journey > Build Windows prototype** genera `Builds/SenderoDorado/SenderoDorado.exe`. Conserva junto al ejecutable la carpeta `SenderoDorado_Data`. `Builds` y `Library` están excluidos de Git.
El paquete listo para trasladar se encuentra en `output/build/PA4_Ecos_del_Bosque_Windows.zip`.

## Entrega PA4

El informe de cinco páginas está en `output/pdf/PA4_Informe_Sendero_Dorado.pdf` y el video narrado de **5:35** en `output/video/PA4_Demostracion_Ecos_del_Bosque.mp4`. La [rama final en GitHub](https://github.com/asrf17/PA3_DV/tree/pa4-final) está publicada en un repositorio público; se verificó el 11 de octubre de 2026. La rama predeterminada aún es `feature/gameplay-3d`, así que conviene entregar el enlace directo a la rama final.

El video enseña exploración, hallazgo de santuarios, recolección, IA, UI, audio, efectos, victoria/derrota y Object Pooling. La recogida final se acelera en la demostración para mostrar el desenlace. La narración actual es sintética: si la rúbrica exige que todos los integrantes expliquen el proyecto con su voz, el equipo debe reemplazarla y añadir nombres y roles antes de presentar. [Guion y tiempos](GUION_VIDEO_PA4.md).
