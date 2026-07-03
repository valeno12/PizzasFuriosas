<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import Cropper from 'cropperjs'
import 'cropperjs/dist/cropper.css'
import AppDrawer from '../ui/AppDrawer.vue'
import UiButton from '../ui/UiButton.vue'
import UiInput from '../ui/UiInput.vue'
import UiSelect from '../ui/UiSelect.vue'
import UiSwitch from '../ui/UiSwitch.vue'

const props = defineProps({
  open: { type: Boolean, default: false },
  product: { type: Object, default: null },
  categories: { type: Array, default: () => [] },
  isSaving: { type: Boolean, default: false },
  errorMessage: { type: String, default: '' },
})
const emit = defineEmits(['close', 'save', 'create-category'])

const form = ref({ name: '', price: '', categoryId: '', isAvailable: true })
const imageFile = ref(null)
const previewUrl = ref('')
const localError = ref('')
const newCategory = ref('')
const showNewCategory = ref(false)

// --- Recorte de imagen (1:1) ---
const isCropping = ref(false)
const cropSourceUrl = ref('')
const cropperImageRef = ref(null)
let cropperInstance = null

const isEditing = computed(() => Boolean(props.product))
const categoryOptions = computed(() =>
  props.categories.map((cat) => ({ value: cat.id, label: cat.name })),
)

watch(
  () => props.open,
  (open) => {
    if (!open) {
      cancelCrop()
      return
    }
    localError.value = ''
    imageFile.value = null
    previewUrl.value = props.product?.image || ''
    isCropping.value = false
    form.value = props.product
      ? {
          name: props.product.name || '',
          price: props.product.price || '',
          categoryId: props.product.categoryId || '',
          isAvailable: props.product.isAvailable !== false,
        }
      : {
          name: '',
          price: '',
          categoryId: props.categories[0]?.id || '',
          isAvailable: true,
        }
  },
)

// El cropper se instancia recién cuando su contenedor está en el DOM.
watch(isCropping, async (cropping) => {
  if (cropping) {
    await nextTick()
    if (cropperImageRef.value) {
      cropperInstance = new Cropper(cropperImageRef.value, {
        aspectRatio: 1,
        viewMode: 1,
        dragMode: 'move',
        autoCropArea: 1,
        highlight: false,
        toggleDragModeOnDblclick: false,
      })
    }
  } else if (cropperInstance) {
    cropperInstance.destroy()
    cropperInstance = null
  }
})

function onFileChange(event) {
  const file = event.target.files?.[0]
  if (!file) return
  if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
    localError.value = 'Usá una imagen JPG, PNG o WEBP.'
    return
  }
  if (file.size > 5 * 1024 * 1024) {
    localError.value = 'La imagen no puede superar 5 MB.'
    return
  }

  cropSourceUrl.value = URL.createObjectURL(file)
  isCropping.value = true
  // Permite volver a elegir el mismo archivo si se cancela el recorte.
  event.target.value = ''
}

function confirmCrop() {
  if (!cropperInstance) return

  const canvas = cropperInstance.getCroppedCanvas({
    width: 800,
    height: 800,
    imageSmoothingEnabled: true,
    imageSmoothingQuality: 'high',
  })

  canvas.toBlob(
    (blob) => {
      if (!blob) {
        localError.value = 'No se pudo recortar la imagen.'
        isCropping.value = false
        return
      }
      imageFile.value = new File([blob], 'product-image.jpg', { type: 'image/jpeg' })
      previewUrl.value = URL.createObjectURL(blob)
      isCropping.value = false
    },
    'image/jpeg',
    0.85,
  )
}

function cancelCrop() {
  isCropping.value = false
  cropSourceUrl.value = ''
}

function submit() {
  localError.value = ''
  if (!form.value.name.trim() || Number(form.value.price) <= 0 || !form.value.categoryId) {
    localError.value = 'Completá nombre, precio mayor a cero y categoría.'
    return
  }
  emit('save', {
    name: form.value.name.trim(),
    price: Number(form.value.price),
    categoryId: Number(form.value.categoryId),
    isAvailable: form.value.isAvailable,
    imageFile: imageFile.value,
  })
}

function createCategory() {
  const name = newCategory.value.trim()
  if (!name) return
  emit('create-category', name)
  newCategory.value = ''
  showNewCategory.value = false
}
</script>

<template>
  <AppDrawer
    :open="open"
    :title="isEditing ? 'Editar producto' : 'Nuevo producto'"
    :description="
      isCropping
        ? 'Ajustá la imagen (1:1)'
        : 'Datos comerciales, categoría, disponibilidad e imagen.'
    "
    @close="emit('close')"
  >
    <!-- Vista de recorte -->
    <div v-if="isCropping" class="flex h-full flex-col gap-4">
      <div class="max-h-[500px] min-h-[300px] flex-1 overflow-hidden rounded-xl bg-black/20">
        <img
          ref="cropperImageRef"
          :src="cropSourceUrl"
          alt="Recortar imagen"
          class="block max-w-full"
        />
      </div>
      <div class="flex justify-end gap-3 pt-2">
        <UiButton variant="secondary" @click="cancelCrop">Cancelar recorte</UiButton>
        <UiButton variant="primary" @click="confirmCrop">Aplicar imagen</UiButton>
      </div>
    </div>

    <!-- Formulario -->
    <form v-else class="grid gap-4" @submit.prevent="submit">
      <div
        v-if="localError || errorMessage"
        class="rounded-2xl bg-danger-soft p-3 text-sm font-semibold text-danger"
        role="alert"
      >
        {{ localError || errorMessage }}
      </div>

      <section class="grid gap-4 sm:grid-cols-[148px_1fr]">
        <!-- Imagen -->
        <div>
          <span class="mb-2 block text-[0.86rem] font-bold text-foreground">Imagen</span>
          <div class="aspect-square overflow-hidden rounded-3xl border border-line bg-surface-2">
            <img
              v-if="previewUrl"
              :src="previewUrl"
              alt="Vista previa del producto"
              class="h-full w-full object-cover"
            />
            <div v-else class="grid h-full place-items-center text-muted">
              <span class="material-symbols-outlined !text-4xl">image</span>
            </div>
          </div>
          <label
            class="mt-3 inline-flex min-h-[40px] w-full cursor-pointer items-center justify-center rounded-[10px] border border-line bg-surface-2 px-3.5 text-[0.875rem] font-bold transition hover:border-line-strong"
          >
            Cambiar
            <input
              class="sr-only"
              type="file"
              accept="image/jpeg,image/png,image/webp"
              @change="onFileChange"
            />
          </label>
          <p class="mt-2 text-center text-xs text-muted sm:text-left">
            JPG, PNG o WEBP hasta 5 MB.
          </p>
        </div>

        <!-- Campos -->
        <div class="grid content-start gap-4">
          <UiInput
            v-model="form.name"
            label="Nombre"
            required
            maxlength="150"
            placeholder="Muzzarella grande"
          />
          <UiInput
            v-model="form.price"
            label="Precio"
            required
            type="number"
            inputmode="numeric"
            min="0"
            step="0.01"
            placeholder="0"
          />

          <UiSelect
            v-if="!showNewCategory"
            v-model="form.categoryId"
            label="Categoría"
            required
            :options="categoryOptions"
            placeholder="Seleccionar categoría"
          />
          <div v-else class="grid gap-2">
            <UiInput
              v-model="newCategory"
              label="Nueva categoría"
              placeholder="Pizzas especiales"
            />
            <div class="flex gap-2">
              <UiButton variant="secondary" @click="showNewCategory = false">Cancelar</UiButton>
              <UiButton variant="primary" @click="createCategory">Crear categoría</UiButton>
            </div>
          </div>
          <button
            v-if="!showNewCategory"
            type="button"
            class="justify-self-start text-sm font-bold text-primary"
            @click="showNewCategory = true"
          >
            Crear categoría nueva
          </button>

          <UiSwitch
            v-if="isEditing"
            v-model="form.isAvailable"
            label="Disponible para venta"
            helper="Si está desactivado, no aparece para cargar pedidos."
          />
        </div>
      </section>
    </form>

    <template v-if="!isCropping" #footer>
      <UiButton variant="secondary" :disabled="isSaving" @click="emit('close')">Cancelar</UiButton>
      <UiButton variant="primary" :loading="isSaving" @click="submit">{{
        isEditing ? 'Guardar cambios' : 'Crear producto'
      }}</UiButton>
    </template>
  </AppDrawer>
</template>
