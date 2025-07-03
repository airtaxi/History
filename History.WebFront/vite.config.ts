import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    // vue() 함수를 아래와 같이 객체 형태로 바꾸고 옵션을 추가합니다.
    vue({
      template: {
        compilerOptions: {
          // 'emoji-picker'로 시작하는 태그를 커스텀 엘리먼트로 처리
          isCustomElement: (tag) => tag.startsWith('emoji-picker')
        }
      }
    }),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
})