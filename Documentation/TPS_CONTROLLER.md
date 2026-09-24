# Controlador TPS

El movimiento anterior por Rigidbody se conserva: aceleración, pendientes, gravedad, colisiones continuas y recuperación de caídas. La escena `Assets/Scenes/Gameplay.unity` y el prefab `Assets/Prefabs/Gameplay/PlayerSphere.prefab` ya están configurados.

## Uso

- WASD o flechas: desplazamiento relativo a la dirección horizontal de la cámara. W avanza, S retrocede y A/D se desplazan lateralmente.
- Mouse: órbita horizontal y vertical alrededor del personaje, con el cursor bloqueado y oculto durante la partida.
- ESC: pausa, libera el cursor y permite usar los menús. Continuar o ESC restaura el bloqueo.
- La esfera gira su orientación hacia el movimiento. La pequeña flecha dorada sobre ella muestra esa orientación; las franjas de la pelota continúan rodando según la distancia recorrida.
- Mirar alrededor estando quieto no obliga al personaje a girar. Moverse tampoco gira automáticamente la cámara.

## Parámetros del Inspector

| GameObject / componente | Campo | Valor inicial / unidad |
| --- | --- | --- |
| Player Sphere / PlayerController | Speed | 14 unidades/s; conserva el valor anterior |
| Player Sphere / PlayerController | Acceleration | 32 unidades/s² |
| Player Sphere / PlayerController | Rotation Speed | 540 grados/s |
| Gameplay Camera / CameraController | Horizontal Sensitivity | 0,14 grados por píxel |
| Gameplay Camera / CameraController | Vertical Sensitivity | 0,12 grados por píxel |
| Gameplay Camera / CameraController | Minimum / Maximum Pitch | −25° / 65° |
| Gameplay Camera / CameraController | Initial Pitch | 18° |
| Gameplay Camera / CameraController | Distance | 7 unidades de brazo de cámara |
| Gameplay Camera / CameraController | Height | 1,6 unidades sobre el centro del jugador; altura del pivote |
| Gameplay Camera / CameraController | Shoulder Offset | 0,55 unidades a la derecha |
| Gameplay Camera / CameraController | Follow Time | 0,08 s de suavizado de seguimiento |
| Gameplay Camera / CameraController | Rotation Smooth Time | 0,035 s de suavizado de órbita |
| Gameplay Camera / CameraController | Collision Radius / Padding | 0,28 / 0,08 unidades |

Un Pitch positivo mira hacia abajo; el negativo permite mirar hacia arriba. Se restringen los límites configurables al intervalo seguro de −80° a 80°. La distancia real se reduce frente a obstáculos y se recupera suavemente al despejarse.

## Responsabilidades y ausencia de conflictos

`PlayerController` captura teclado en Update. En FixedUpdate aplica las fuerzas que ya existían y utiliza Rigidbody.MoveRotation para orientar al personaje suavemente. La interpolación del Rigidbody permanece activada; no se modifica su Transform para moverlo ni orientarlo desde Update.

`CameraController` captura desplazamiento de mouse en Update. Ese desplazamiento ya representa píxeles por fotograma, por lo que no se multiplica por deltaTime. En LateUpdate sigue la posición interpolada del personaje y resuelve la órbita y la colisión mediante SphereCast. El primer delta tras recuperar el cursor se descarta para evitar un salto por recentrado.

`BallRollingVisual` conserva la rotación visual de la pelota en coordenadas del mundo, independientemente del giro horizontal del cuerpo físico. `Facing Indicator` es un hijo separado que sí sigue la orientación del personaje. No se requieren Cinemachine ni paquetes adicionales.

`GameManager` controla el cursor según el estado. `PauseMenu` conserva ESC, reinicio y pausa al perder el foco. La brújula continúa usando la orientación real de la cámara.

## Preparación para apuntado

El apuntado todavía no tiene una tecla, arma o mira asignada. Un futuro componente de entrada puede activar la orientación de apuntado con:

```csharp
player.SetAiming(isAimHeld);
Ray aimRay = followCamera.AimRay;
```

Mientras IsAiming es true, el personaje gira suavemente hacia el yaw de la cámara incluso estando quieto, y puede retroceder o desplazarse lateralmente manteniendo esa orientación. Al desactivarlo, vuelve a orientarse hacia el movimiento. Pausar o reiniciar desactiva este estado. `SetOrbit(yaw, pitch, immediate)` permite transiciones de cámara o un encuadre de apuntado futuro sin sustituir el controlador.

## Verificación y respaldo

**Tools > Forest Journey > Verify TPS in Play Mode** prueba el controlador mediante un teclado y mouse virtuales dentro de Unity. Verifica WASD con cámaras a 90° y 180°, rotación del personaje, independencia de órbita, sensibilidad, límites de Pitch, apuntado futuro, cursor, colisiones de cámara, estabilidad en reposo y reinicio. Termina en el menú y restaura los dispositivos de entrada de la sesión. Ejecuta cada suite individualmente.

El informe está en `Library/TPSVerification.txt`; la captura en `Library/TPSGameplay.png`. La suite general anterior sigue disponible para comprobar monedas, pendientes, colisiones, contador y menús.

La etiqueta Git `gameplay-before-tps-20260924` conserva la versión funcional anterior, commit `7f2e284`. El desarrollo continúa en `feature/gameplay-3d`, sin modificar ni integrar cambios en `main`.
