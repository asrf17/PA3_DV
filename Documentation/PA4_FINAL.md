# Producto Académico 4 — Sendero Dorado: Ecos del Bosque

## Juego y objetivo

Sendero Dorado es una aventura breve de exploración fantástica 3D. La esfera viajera debe recuperar diez reliquias de luz en distintas zonas de un bosque encantado. También puede descubrir tres santuarios opcionales: Santuario del Alba, Mirador del Viento y Ruinas del Eco. Al acercarse a cada uno, el HUD registra el hallazgo. El guardián de las ruinas patrulla cuatro puntos y persigue al jugador cuando lo ve. Hay siete minutos y tres oportunidades; el contacto con el guardián quita una y devuelve al jugador al inicio con protección breve. Se gana al reunir las diez reliquias y se pierde al agotar tiempo u oportunidades. Las pantallas finales permiten reiniciar o volver al menú.

La inspiración es la sensación de exploración de aventuras fantásticas de mundo abierto. Personajes, modelos sencillos, historia, nombres y recursos son propios de este proyecto; no se usan personajes ni arte de otra franquicia.

## Abrir y jugar

1. Abre el proyecto con Unity **6000.6.0f1** y la rama `codex/pa4-final`.
2. Abre `Assets/Scenes/Gameplay.unity`, pulsa **Play** y selecciona **Jugar**.
3. Usa **WASD** o las flechas para mover la esfera respecto a la cámara; mueve el mouse para mirar alrededor. La flecha superior señala aproximadamente la reliquia pendiente más cercana.
4. Entra en los círculos de las piedras antiguas para descubrir los santuarios. Son opcionales y se reinician al comenzar otra expedición.
5. Pulsa **Escape** para pausar. La pausa libera el cursor y detiene al personaje, el tiempo y la IA.

## Sistemas y decisiones técnicas

- `PlayerController` conserva el Rigidbody, la detección de suelo, aceleración, respuesta a pendientes y giro hacia la marcha.
- `CameraController` sigue a la esfera con órbita independiente, limita el Pitch y se acerca cuando encuentra obstáculos.
- `Coin` y `CoinManager` mantienen los Triggers y el registro interno del sistema original. En pantalla se presentan como reliquias. La recolección es única y se restablece al reiniciar.
- `ExplorationLandmark` y `DiscoveryJournal` añaden tres lugares opcionales. Sus Triggers registran el hallazgo solo una vez; el contador y nombre aparecen en `ExpeditionHUD`. El registro se restablece en una nueva partida.
- `GameManager` controla menú, juego, pausa, victoria, derrota, tiempo, oportunidades y reinicios.
- `ForestGuardian` alterna entre **Patrol** y **Chase**. Recorre cuatro waypoints, usa Raycast para comprobar visión y SphereCast para evitar obstáculos. Actualiza la percepción cada 0,2 segundos.
- `CollectionFeedback` reproduce una campanada por reliquia y reutiliza cuatro sistemas de partículas. Este **Object Pooling** evita crear y destruir partículas en cada recogida. Usa tiempo no escalado para conservar el último efecto al ganar.
- `Forest Ambience` reproduce viento y pájaros sintetizados. El nivel tiene Terrain, relieve, vegetación, materiales, iluminación URP y Volume global. Las reliquias combinan cristales luminosos y un Shader Graph propio con pulso de emisión. Tres piedras antiguas con luces suaves crean puntos de interés; se despejaron pequeños claros sin eliminar el bosque.
- `PlayerSphere`, `GoldenCoin`, `ForestGuardian` y `AncientWaystone` son Prefabs reutilizables.

## Pruebas y compilación

En Play Mode, el menú **Tools > Forest Journey** ejecuta las suites:

- **Verify gameplay in Play Mode**: 41 comprobaciones de movimiento, colisiones, Triggers, menú, pausa, victoria y reinicio.
- **Verify TPS in Play Mode**: 43 comprobaciones de cámara, WASD relativo, giro, cursor y estabilidad.
- **Verify PA4 in Play Mode**: 35 comprobaciones de patrulla, persecución, contacto, tiempo, resultados, UI, audio, pool y descubrimiento de santuarios tanto por lógica como por Trigger real.

Los informes y capturas se guardan localmente en `Library`. Las 119 comprobaciones automatizadas incluyen traslados directos para cubrir todos los Triggers; conviene recorrer también el bosque manualmente para valorar la experiencia. La validación previa al Build comprueba referencias, Triggers, santuarios, HUD y ausencia de scripts faltantes.

**Tools > Forest Journey > Build Windows prototype** genera `Builds/SenderoDorado/SenderoDorado.exe`. Conserva junto al ejecutable la carpeta `SenderoDorado_Data`. `Builds` y `Library` están excluidos de Git.
El paquete listo para trasladar se encuentra en `output/build/PA4_Ecos_del_Bosque_Windows.zip`.

## Entrega PA4

El informe de cinco páginas está en `output/pdf/PA4_Informe_Sendero_Dorado.pdf` y el video narrado de **5:35** en `output/video/PA4_Demostracion_Ecos_del_Bosque.mp4`. La [rama final en GitHub](https://github.com/asrf17/PA3_DV/tree/codex/pa4-final) está publicada en un repositorio público; se verificó el 10 de octubre de 2026. La rama predeterminada aún es `feature/gameplay-3d`, así que conviene entregar el enlace directo a la rama final.

El video enseña exploración, hallazgo de santuarios, recolección, IA, UI, audio, efectos, victoria/derrota y Object Pooling. La recogida final se acelera en la demostración para mostrar el desenlace. La narración actual es sintética: si la rúbrica exige que todos los integrantes expliquen el proyecto con su voz, el equipo debe reemplazarla y añadir nombres y roles antes de presentar. [Guion y tiempos](GUION_VIDEO_PA4.md).
