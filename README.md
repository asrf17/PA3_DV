# Sendero Dorado: Ecos del Bosque — evaluación final PA4

Aventura original de exploración y supervivencia 3D hecha en Unity 6000.6.0f1. La esfera viajera recupera doce reliquias de luz en etapas de 3, 4 y 5, y puede descubrir tres santuarios. La partida empieza con dos guardianes; cada reliquia añade otro cerca del siguiente objetivo y los cambios de etapa suman refuerzos. El personaje tiene 100 puntos de vida, pierde 25 por contacto y recupera 35 con un botiquín. Hay siete minutos para completar la aventura.

Abre `Assets/Scenes/Gameplay.unity`, pulsa **Play** y elige **Jugar**. Muévete con WASD o flechas en relación con la cámara, salta con **Espacio** y usa el mouse para observar. **Escape** pausa y libera el cursor. La flecha indica el rumbo aproximado de la reliquia disponible más cercana. Los botiquines verdes curan cuando falta vida y los cristales celestes aumentan la velocidad durante ocho segundos. La barra inferior muestra la vida y una señal roja indica de dónde llega el daño. Los controles aparecen en la pausa, sin tapar la vista durante la partida. Recuperar las doce reliquias produce la victoria; perder toda la vida o agotar el tiempo produce la derrota. Se puede reintentar desde la pantalla final.

- `main`: mapa original.
- `feature/gameplay-3d`: personaje, cámara, coleccionables, brújula e interfaces.
- `pa4-final`: experiencia PA4 con guardianes crecientes, etapas, botiquines, santuarios, audio, efectos, finales y pruebas.

La esfera, las reliquias luminosas, los guardianes, botiquines, pasajes y piedras antiguas son Prefabs. El brillo de las reliquias utiliza un Shader Graph propio. Los efectos de recolección reutilizan cuatro sistemas de partículas mediante Object Pooling.

Consulta [GAMEPLAY.md](GAMEPLAY.md) para conocer los componentes, controles y pruebas. Los recursos grandes usan Git LFS. `Library`, `Temp`, `Logs` y `Builds` no se publican en Git.

Descarga el [juego Windows comprimido](output/build/PA4_Ecos_del_Bosque_Windows.zip) para ejecutarlo fuera de Unity.

La versión final está publicada en la [rama pa4-final de GitHub](https://github.com/asrf17/PA3_DV/tree/pa4-final). La rama predeterminada del repositorio todavía muestra la versión anterior.
