# Cámaras de seguridad

## Agregar una cámara

En Unity, usar **GameObject > Silent Files > Security Camera**. Crea un pivote con
`FieldOfView`, `SecurityCamera`, `CameraRotation` y `CameraVisual`. También se pueden
agregar esos cuatro componentes manualmente al mismo GameObject. No necesita un
componente `Camera` de Unity: es un sensor, no una segunda cámara de renderizado.

Orientar el eje local **+Z** hacia el centro del recorrido. El pivote es el origen
de detección y del haz 3D; mantenerlo fuera de las paredes. Se puede ubicar a la
altura de la cámara e inclinarlo hacia el área vigilada: el eje +Z debe apuntar
hacia donde se quiere mirar. Se conserva la detección 3D del FOV original: la
altura también interviene en distancia, ángulo y raycast. El modelo visible puede
ser un hijo. El cono conserva su radio en unidades del mundo incluso si el modelo
tiene escala.

| Componente | Inspector |
| --- | --- |
| `FieldOfView` | `Radius`: alcance. `Angle`: apertura completa. `Player Ref`: GameObject raíz del jugador; vacío conserva la búsqueda existente por tag `Player`. `Target Mask`: `Target`. `Obstruction Mask`: `Obstruction`. |
| `SecurityCamera` | `Guards`: arrastrar los componentes `AI` de todos los guardias que deben responder. No busca guardias globalmente. `On Player Detected`: evento opcional con la posición para conectar una futura alarma general. |
| `CameraRotation` | `Minimum Angle` / `Maximum Angle`: offsets en grados respecto de la orientación inicial, por ejemplo -45 y 45. `Rotation Speed`: grados/segundo. `Start Towards Maximum`: dirección inicial. |
| `CameraVisual` | `Normal Color` y `Detected Color` incluyen transparencia. `Cone Material` vacío usa el material URP incluido. `Arc Segments` y `Radial Segments` controlan la resolución del volumen y su recorte. `Obstruction Padding` deja una pequeña separación frente a los obstáculos. |

`SecurityCamera` activa automáticamente el modo **Instant** del FOV. En ese modo
`Delay`, `Suspicion` y `Seguir Jugador Script` no intervienen; dejar esta última
referencia vacía en las cámaras. El barrido comienza en offset 0, limitado al
intervalo configurado. Con velocidad 0 queda inmóvil; con límites iguales queda
en ese ángulo. Desactivar `CameraRotation` permite usar un sensor fijo.

## Jugador, paredes y guardias

- El jugador necesita un Collider o CharacterController en la capa **Target**.
  También se admiten colliders en hijos del `Player Ref`; asignar la capa a esos
  hijos. El tag `Player` es necesario sólo si no se asigna `Player Ref`.
- Paredes y puertas que bloquean la vista necesitan colliders sólidos en la capa
  **Obstruction**, o en otra capa incluida en `Obstruction Mask`. Los triggers no
  bloquean la visión ni el haz. La misma configuración recorta el cono 3D: incluir
  también las capas del piso, techo u objetos si deben bloquearlo. No incluir al
  propio jugador ni al modelo de la cámara en esa máscara.
- Cada guardia conserva su `AI`, `NavMeshAgent`, destinos, FOV y configuración de
  puertas. El agente debe estar sobre un NavMesh activo y horneado. La alerta usa
  `Sound Nav Mesh Search Radius` y `Sound Arrival Distance`, compartidos con la
  investigación sonora existente.
- Dejar **Follow Player** desactivado para guardias que sólo conocen al jugador
  por detección: es una opción anterior de persecución por proximidad. Se inhibe
  durante una alerta de cámara, pero conserva su comportamiento al finalizarla.

El mesh visual se genera automáticamente, sin Collider ni MeshCollider, sobre
**Ignore Raycast**. La cámara de gameplay debe renderizar esa capa (las cámaras
actuales de `Test-Mecanicas` ya la incluyen). No agregar componentes físicos al
cono. Para reemplazar el material, usar uno transparente que admita `_BaseColor`
o `_Color`; el material compartido no se modifica, cada cono tiene su propio color.

## Flujo y compatibilidad

1. `CameraRotation` actualiza la orientación usando `Time.deltaTime`.
2. El FOV de la cámara evalúa en cada `LateUpdate` radio, ángulo, capa y raycast,
   sin espera ni acumulación de sospecha. Publica `VisibilityChanged` al cambiar.
3. `CameraVisual` pasa inmediatamente a rojo o vuelve al color normal. El mismo
   mesh se muestra en Scene View y Game View; en edición se previsualiza el cono
   y sus parámetros, y en Play Mode se observa la detección real.
4. `SecurityCamera` copia `playerRef.transform.position` en cada nueva detección,
   llama `AI.ReportPlayerDetection(Vector3)` para cada guardia y emite el evento.
   Una referencia duplicada a un guardia no duplica el aviso.
5. `AI` investiga esa posición con su `NavMeshAgent`, reutilizando el cálculo de
   rutas y la finalización de investigación ya utilizados para sonidos. La alerta
   interrumpe patrulla/descanso/sonidos; la visión confirmada del propio guardia
   conserva prioridad. `GuardDoorPassage` sigue resolviendo las puertas.

Mientras la cámara mantiene visión continua no actualiza la posición conocida
de los guardias, aunque el jugador se mueva. Perderlo y volver a verlo genera un
nuevo aviso. Perder visión no cancela una investigación ya comunicada. Al llegar
sin detectar al jugador, el guardia retoma la patrulla. Si no hay ruta completa,
también retoma la patrulla; si el agente aún no está listo, conserva el aviso.

Los FOV existentes siguen en `GuardSuspicion` por defecto, con su intervalo y
sospecha originales. Se mantienen nombres y referencias serializadas anteriores.
El visual es un volumen cónico cerrado, con alcance esférico igual al radio del
FOV. Sus caras se recortan usando las consultas a obstáculos compartidas con el
FOV. Estas consultas visuales sólo calculan geometría; la detección del jugador y
las notificaciones siguen perteneciendo al sistema existente.

El recorte se actualiza al rotar la cámara y al mover, desactivar o abrir un
obstáculo, tanto en Scene View como en juego. Aumentar la resolución permite
representar mejor los bordes y obstáculos pequeños, a costa de más consultas de
física por cámara. El haz es una representación visual del campo de visión; no
agrega una luz `Spot Light` ni modifica la iluminación de los materiales del nivel.

## Verificación

En **Window > General > Test Runner > EditMode**, ejecutar:

- `SecurityCameraTests`: detección instantánea, radio/ángulo/máscaras, colliders
  hijos, obstáculos, color, aviso único y recaptura, múltiples guardias, destino
  fijo y desplazamiento real del `NavMeshAgent`.
- `CameraPresentationTests`: inversión del barrido, independencia del framerate,
  límites especiales, volumen 3D, orientación, radio del mesh, material, colores,
  limpieza y recorte contra paredes y obstáculos móviles/parciales.
- `GuardCameraAlertTests`: prioridades, agente temporalmente deshabilitado,
  llegada, rutas imposibles y espera de puertas durante alertas.
- `DoorGuardPassageTests` y `GuardDoorNavigationTests`: regresión del sistema de
  puertas anterior.

Las pruebas de integración entran y salen de Play Mode y construyen su propio
NavMesh. Para verificar el nivel concreto, agregar la cámara, asignar `Guards`,
activar su NavMesh y cruzar el cono: amarillo → rojo; detrás de una pared → amarillo;
el guardia va a la posición informada. Salir y volver a entrar debe renovar el aviso.
En `Test-Mecanicas`, el objeto `NavMesh Surface` estaba desactivado: activarlo y
comprobar el horneado antes de esta prueba manual.

La validación se ejecuta con Unity **6000.5.7f1**, sobre una copia aislada del
proyecto. Incluye compilación C#, pruebas de integración y renderizado con
Direct3D11 y el renderer URP actual.

Los resultados locales están en
`.utmp/security-camera-validation/security-camera-tests-final.xml` y las capturas
en `.utmp/security-camera-validation/security-camera-render/` (archivos temporales
ignorados por Git). La importación limpia también informó errores preexistentes
en variantes de shaders de TextMesh Pro dentro de `Assets/TextMesh Pro/Shaders`,
por el include `TMPro_Properties.cginc`; no afectan estas pruebas y no se modificaron.
El shader del cono se compiló y renderizó correctamente.
