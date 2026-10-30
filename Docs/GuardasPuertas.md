# Guardas y puertas

`AI` y `NormalGuard` agregan automáticamente `GuardDoorPassage` al objeto del
`NavMeshAgent`. El guarda solicita las puertas que encuentra en su recorrido
próximo, espera que estén abiertas y libera cada una después de despejar el hueco.
Funciona durante patrulla, investigación de sonidos y persecución.

La primera solicitud guarda el `estadoActual` de la puerta. Al terminar el último
guarda, la puerta recupera ese estado: `Cerrada`, `Trabada` o `Bloqueada`. Si ya
estaba `Abierta`, permanece abierta. También se libera una solicitud si el guarda
cambia de recorrido o se desactiva. Mientras haya guardas usando la puerta, la
interacción del jugador no puede cerrarla ni cambiar su cerradura. El resultado de
una ganzúa iniciada antes del paso se descarta si la puerta cambió entretanto.

## Configuración actual, sin animación

- Mantener **Esperar Eventos Animacion** desactivado. La apertura y el cierre son
  inmediatos y liberan/restauran los colliders; la hoja todavía no se mueve.
- El componente `Door` debe estar en cada puerta utilizable. **Colliders Bloqueo**
  vacío encuentra los colliders sólidos de la hoja y sus hijos. Si el marco también
  es hijo, asignar explícitamente solo los colliders de la hoja.
- **Volumen Paso** toma por defecto el primer `BoxCollider` de la hoja. En puertas
  con varias hojas o geometría especial, asignar un `BoxCollider` que cubra todo el
  hueco cerrado, preferentemente un trigger independiente. Se guarda su posición
  inicial para que animar la hoja no cambie la comprobación de paso.
- El NavMesh debe tener un recorrido a través del hueco: hornear el suelo con la
  hoja excluida de la geometría de navegación o configurar un enlace que lo cruce.
  No usar un obstáculo con carving que elimine ese recorrido. Desactivar un collider
  en ejecución no reconstruye el NavMesh horneado.

En `Test-Mecanicas.unity`, el objeto `NavMesh Surface` ya estaba desactivado y los
destinos de patrulla están lejos de `TestDoor`. Para una prueba manual, activar la
navegación, comprobar el horneado y colocar destinos a ambos lados de la puerta.

## Conectar la futura animación

1. Asignar **Animator Puerta**. Si comparte objeto con `Door`, se encuentra
   automáticamente. Crear en el controller un parámetro bool **Abierta** (o cambiar
   **Parametro Abierta**) y usarlo para las transiciones de apertura y cierre.
2. Activar **Esperar Eventos Animacion** en `Door`.
3. En el clip de apertura, agregar un Animation Event `NotificarPuertaAbierta`
   cuando la hoja ya deje pasar. Hasta entonces, el guarda permanece detenido y el
   collider sigue bloqueando.
4. Al final del clip de cierre, agregar `NotificarPuertaCerrada`. Esto restaura los
   colliders. Configurar el controller para poder volver a abrir si llega otro
   guarda mientras está cerrando.

Los Animation Events se entregan al objeto que tiene el Animator: para llamar
directamente esos métodos, debe ser el mismo objeto que contiene `Door`. Para un
Animator hijo, Timeline u otro controlador, conectar **Al Solicitar Apertura** y
**Al Solicitar Cierre** y reenviar las notificaciones a la instancia de `Door`.
Los métodos de notificación ignoran señales que no corresponden a la fase actual.
La pose inicial del controller debe respetar el bool `Abierta`.

No se agregaron clips ni se modificó `PuertasAnimator.controller`.

## Verificación

Las pruebas de `Assets/Tests/Editor` cubren restauración de estados, reservas
simultáneas, callbacks de animación, geometría del hueco y cruce con un NavMesh
generado durante la prueba. Se ejecutan desde la pestaña EditMode del Test Runner;
los casos de navegación entran y salen de Play Mode por sí mismos.
