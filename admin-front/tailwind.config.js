/** @type {import('tailwindcss').Config} */

// Los colores viven como variables CSS en src/assets/main.css (única fuente de verdad).
// Acá solo se exponen a Tailwind para poder usar bg-surface, text-muted, border-line, etc.
export default {
  content: ['./index.html', './src/**/*.{vue,js}'],
  theme: {
    extend: {
      colors: {
        background: 'var(--background)',
        foreground: 'var(--foreground)',
        surface: {
          DEFAULT: 'var(--surface)',
          2: 'var(--surface-2)'
        },
        popover: 'var(--popover)',
        field: 'var(--field)',
        primary: {
          DEFAULT: 'var(--primary)',
          foreground: 'var(--primary-foreground)',
          soft: 'var(--primary-soft)'
        },
        muted: 'var(--muted-foreground)',
        subtle: 'var(--subtle-foreground)',
        line: {
          DEFAULT: 'var(--line)',
          soft: 'var(--line-soft)',
          strong: 'var(--line-strong)'
        },
        danger: {
          DEFAULT: 'var(--danger)',
          foreground: 'var(--danger-foreground)',
          soft: 'var(--danger-soft)'
        },
        success: {
          DEFAULT: 'var(--success)',
          soft: 'var(--success-soft)'
        },
        warning: {
          DEFAULT: 'var(--warning)',
          soft: 'var(--warning-soft)'
        },
        info: {
          DEFAULT: 'var(--info)',
          soft: 'var(--info-soft)'
        }
      },
      fontFamily: {
        display: ['"Caveat Brush"', 'cursive'],
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif']
      },
      keyframes: {
        shimmer: {
          '100%': { transform: 'translateX(100%)' }
        }
      },
      animation: {
        shimmer: 'shimmer 1.25s infinite'
      }
    }
  },
  plugins: []
}
