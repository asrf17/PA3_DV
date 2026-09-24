# Verificación del prototipo

Fecha: 23 de septiembre de 2026. Editor: Unity 6000.6.0f1, Windows.

## Resultado de integración

La ejecución de `GameplayVerification` terminó con **38 comprobaciones aprobadas** y sin errores ni excepciones durante la prueba.

- Inicio desde el botón Jugar, personaje apoyado en el terreno y contador en cero.
- WASD y flechas usando un teclado virtual del Input System.
- Rodamiento visual durante el desplazamiento.
- Colisión contra una pared sólida y ascenso de una rampa de doce grados con físicas reales.
- ESC pausa tiempo, movimiento y físicas; Continuar restaura la partida.
- Activación de los diez triggers y rechazo de una segunda recolección de cada moneda.
- Selección de la moneda más cercana, cambio de objetivo tras recogerla y ocultación de la flecha al terminar.
- Contador final 10/10 y mensaje de objetivo completado.
- Reinicio, regreso al menú principal y recuperación de caídas fuera del terreno.

La comprobación visual mediante capturas confirmó el menú principal, HUD, flecha y franjas de la esfera. Detectó y permitió corregir un CanvasRenderer faltante en la flecha antes de la ejecución final aprobada.

## Alcance

La colocación encontró 55.212 nodos conectados mediante comprobaciones de pendiente y espacio libre para la esfera. Las diez monedas pertenecen a esa región. Esto verifica conectividad geométrica; no equivale a recorrer manualmente todo el bosque.

La prueba de triggers desplaza al jugador entre monedas para cubrir las diez posiciones. La prueba de movimiento utiliza el terreno real y dos obstáculos temporales, eliminados al terminar.

Los informes detallados y capturas se generan en `Library/GameplayQA.txt` y `Library/Gameplay*.png`, excluidos del repositorio. Puede repetirse la suite desde **Tools > Forest Journey > Verify gameplay in Play Mode**, comenzando en el menú principal.

## Preservación

`main` conserva el commit `448a3200dbc27ff6560e1ebe7ec3ee717f9e88d1`. La escena `SampleScene.unity` y los datos `New Terrain.asset` no tienen diferencias respecto a ese respaldo. Todo el desarrollo posterior pertenece a `feature/gameplay-3d`.
