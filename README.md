# Sendero Dorado: Ecos del Bosque — evaluación final PA4

Aventura original de exploración fantástica 3D hecha en Unity 6000.6.0f1. El jugador guía una esfera viajera por un bosque encantado para recuperar diez reliquias de luz y descubrir tres santuarios. Un guardián patrulla las ruinas y persigue al explorador si lo detecta. La partida tiene menú, pausa, victoria y derrota; dura como máximo siete minutos y comienza con tres oportunidades.

Abre `Assets/Scenes/Gameplay.unity`, pulsa **Play** y elige **Jugar**. Muévete con WASD o flechas en relación con la cámara. Usa el mouse para observar y **Escape** para pausar o liberar el cursor. La flecha indica el rumbo aproximado de la reliquia disponible más cercana. Acércate a los santuarios para registrarlos en el HUD; son hallazgos opcionales que premian la exploración. Recuperar las diez reliquias despierta el santuario y completa la aventura.

- `main`: mapa original.
- `feature/gameplay-3d`: personaje, cámara, coleccionables, brújula e interfaces.
- `codex/pa4-final`: experiencia PA4 completa, con temática fantástica, guardián, santuarios, audio, efectos, finales y pruebas.

La esfera, las reliquias luminosas, el guardián y las piedras antiguas son Prefabs. El brillo de las reliquias utiliza un Shader Graph propio. Los efectos de recolección reutilizan cuatro sistemas de partículas mediante Object Pooling.

Consulta [GAMEPLAY.md](GAMEPLAY.md) para conocer los componentes y controles y [Documentation/PA4_FINAL.md](Documentation/PA4_FINAL.md) para las pruebas y la entrega. Los recursos grandes usan Git LFS. `Library`, `Temp`, `Logs` y `Builds` no se publican en Git.

Entregables: [informe PDF](output/pdf/PA4_Informe_Sendero_Dorado.pdf), [video demostrativo de 5:35](output/video/PA4_Demostracion_Ecos_del_Bosque.mp4) y [juego Windows comprimido](output/build/PA4_Ecos_del_Bosque_Windows.zip). Para la exposición, el equipo debe sustituir la narración sintética del video por sus propias voces e indicar nombres y roles si el docente lo exige.
