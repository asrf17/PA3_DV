# Sendero Dorado - Evaluación Final PA4

Juego breve de exploración 3D construido sobre el mapa original de PA3_DV, con Unity 6000.6.0f1.

Abre `Assets/Scenes/Gameplay.unity`, entra en Play y selecciona **Jugar**. Controla la esfera con WASD o flechas respecto a la cámara y usa el mouse para mirar alrededor con órbita TPS. Encuentra diez monedas en siete minutos y evita al guardián, que patrulla y persigue. Pulsa ESC para pausar y liberar el cursor, reiniciar o regresar al menú. Hay victoria al reunir todas las monedas y derrota al agotar el tiempo o tres oportunidades.

- `main`: respaldo original del mapa, sin desarrollo de gameplay.
- `feature/gameplay-3d`: personaje, cámara, coleccionables, brújula, interfaces y menús.
- `codex/pa4-final`: partida completa PA4, guardián, audio ambiental, finales, optimización y pruebas.

Las monedas incorporan un Shader Graph propio con un pulso de emisión dorada animado mediante tiempo y una onda seno.

Consulta [GAMEPLAY.md](GAMEPLAY.md) para conocer los componentes, referencias del Inspector, controles, pruebas y compilación para Windows.

Consulta [Documentation/PA4_FINAL.md](Documentation/PA4_FINAL.md) para conocer el objetivo, la IA, el pool de partículas, las pruebas y la entrega final.

Los recursos grandes utilizan Git LFS. Library, Temp, Logs y Builds no forman parte del repositorio.
