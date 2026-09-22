# Promociones y actualización de producción

Las promos son productos con precio propio, componentes (producto + cantidad) y una opción de envío gratis. Se pueden agrupar en una categoría «Promos», creada desde el editor. No se crean promos ni se cambian precios automáticamente: los nombres, importes y composiciones se cargan desde Productos → Nuevo producto → Es una promo / combo.

## Datos y compatibilidad

La migración `AddProductPromotions` es aditiva:

- Agrega `Products.FreeDelivery` (false por defecto).
- Agrega `OrderItems.FreeDelivery` (false) y `ProductNameSnapshot` (nullable).
- Crea `ProductComponents` y `OrderItemComponents`, con sus índices y relaciones.
- No borra ni recalcula productos, clientes, pedidos o importes existentes; no cambia las migraciones anteriores.

Los pedidos guardan una línea por promo, con el precio cobrado y una copia de sus componentes **por unidad de promo**, nombres y beneficio de envío. Cocina multiplica esas cantidades por la cantidad de promos. Las estadísticas suman los componentes junto con los productos individuales; el ranking de promos es independiente y los ingresos se computan solamente desde el total del pedido.

Al editar un pedido activo, las promos que ya estaban conservan su composición, nombre, precio y beneficio, incluso si cambió el catálogo. Los productos individuales mantienen el comportamiento anterior de edición. Las nuevas promos usan la configuración vigente. El envío gratis bonifica todo el envío del pedido; al quitar la última promo con ese beneficio, se usa el costo de envío ingresado en el formulario (debe cargarse si el pedido guardado tenía envío cero).

Las actualizaciones de producto de clientes anteriores que omiten `components` y `freeDelivery` conservan esos valores. Para quitar la configuración se envían `components: []` y `freeDelivery: false`. No se admiten promos anidadas ni productos repetidos. Una promo con un componente agotado no aparece en el catálogo disponible y el servidor rechaza nuevas ventas. El switch de la promo controla su habilitación propia. Para borrar un producto usado por una promo vigente, primero hay que quitarlo de la composición.

## Despliegue

1. Obtener un backup de producción y comprobar que se puede restaurar antes de actualizar.
2. Desplegar primero el backend. El arranque actual ya ejecuta `Database.Migrate()`, que aplicará esta migración pendiente. No usar `EnsureDeleted`, recrear la base ni volver a ejecutar seeds de demostración.
3. Verificar la migración y consultar un pedido anterior; después desplegar admin y web pública.
4. Cargar las promos y comprobar un pedido de prueba: detalle de cocina, precio total y envío.

Las pruebas automáticas usan PostgreSQL descartable. Incluyen crear datos con el esquema anterior, aplicar la nueva migración y verificar que el pedido histórico conserve sus cantidades e importes. No acceden a producción.

Si fuera necesario volver al backend anterior, conservar el esquema ampliado: las columnas nuevas tienen valores por defecto y las tablas originales siguen existiendo. No ejecutar `Down` después de vender promos, porque eliminaría la composición histórica. Una versión anterior tampoco entiende el beneficio ni los componentes: no debe editar pedidos con promos ni venderlas durante un rollback.
