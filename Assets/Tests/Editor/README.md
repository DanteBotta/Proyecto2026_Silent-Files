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
