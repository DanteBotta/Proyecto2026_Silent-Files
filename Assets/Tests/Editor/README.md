Pruebas del paso de guardas por puertas
=====================================

En Unity, abrir `Window > General > Test Runner`, seleccionar `EditMode` y ejecutar
`DoorGuardPassageTests` y `GuardDoorNavigationTests`.

La primera suite verifica los cuatro estados originales, reservas simultaneas,
solicitudes repetidas, eventos de animacion y geometria del hueco. Inicializa
explicitamente los metodos de ciclo de vida porque corre en Edit Mode.

La segunda suite entra temporalmente a Play Mode y construye un NavMesh pequeno
en memoria. Comprueba el recorrido real, la espera por la animacion, la liberacion
al desactivar el guarda y la recuperacion cuando se desactiva y reactiva la puerta
en el mismo frame. Incluye un collider de cuerpo mas ancho que el NavMeshAgent.
No necesita cargar una escena del juego ni disponer de clips de puertas.

Los archivos usan `UNITY_INCLUDE_TESTS` y NUnit en `Assembly-CSharp-Editor`, de modo
que no entran en las compilaciones del juego. No hace falta mover los scripts de
produccion a un assembly nuevo.

Para ejecutar desde consola con el proyecto cerrado:

```text
Unity.exe -batchmode -nographics -projectPath "RUTA_AL_PROYECTO" -runTests -testPlatform EditMode -testResults "RUTA_RESULTADOS.xml" -logFile "RUTA_LOG.log"
```

No agregar `-quit`: el Test Runner termina Unity cuando finalizan las pruebas.
Si el proyecto ya esta abierto, ejecutar desde su Test Runner o usar una copia
aislada; no abrirlo simultaneamente en otra instancia del editor.

Camaras de seguridad
====================

Ejecutar tambien `SecurityCameraTests`, `CameraPresentationTests` y
`GuardCameraAlertTests`. Cubren el FOV instantaneo, obstrucciones, color del cono,
rotacion independiente del framerate, eventos sin repeticion, multiples guardias,
posicion conocida fija, movimiento NavMesh, prioridades y compatibilidad con puertas.
La suite de presentacion tambien comprueba el volumen 3D y su recorte contra
paredes planas/oblicuas, objetos parciales y obstaculos que se mueven o desactivan.
Los casos de integracion entran a Play Mode y generan su navegacion en memoria.
La configuracion del sistema esta documentada en `Docs/CamarasSeguridad.md`.
