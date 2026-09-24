# Verificación del prototipo

Fecha de verificación final: 24 de septiembre de 2026. Editor: Unity 6000.6.0f1, Windows.

## Resultado de integración

La ejecución final de `GameplayVerification` terminó con **40 comprobaciones aprobadas** y sin errores ni excepciones durante la prueba.

- Inicio desde el botón Jugar, personaje apoyado en el terreno y contador en cero.
- Navegación real con flechas entre Jugar y Salir, envío con Enter para iniciar y continuación desde la pausa con Enter.
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

## Compilación Windows

La compilación finalizada el 23 de septiembre produjo `Builds/SenderoDorado/SenderoDorado.exe`: **Succeeded, 0 errores, 14 advertencias**, aproximadamente 170 MB según el informe de Unity. El 24 de septiembre se comprobó el arranque independiente y el cierre de la ventana de la aplicación.

Las advertencias incluyen compatibilidad de shaders Polytope originales (`_FORWARD_PLUS`, `UnityGBuffer.hlsl`) y billboards de vegetación. El ejecutable también registra avisos de efectos de posprocesado descartados durante la compilación. No se modificaron los shaders originales para evitar introducir cambios innecesarios en el mapa. Las capturas revisadas en detalle corresponden al modo Play del editor.

Unity actualizó la serialización de ajustes gráficos de la versión 6.6 durante la compilación; estos ajustes se conservaron exclusivamente en la rama de desarrollo. La compilación queda fuera de Git, como requiere `.gitignore`.
