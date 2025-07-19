import { ref, readonly } from 'vue';

export function useLongPress(delay: number = 500) {
  const timer = ref<ReturnType<typeof setTimeout> | null>(null);
  const isLongPressing = ref(false);

  const start = (callback: () => void) => {
    isLongPressing.value = false;
    if (timer.value) {
      clearTimeout(timer.value);
    }
    timer.value = setTimeout(() => {
      isLongPressing.value = true;
      callback();
      timer.value = null;
    }, delay);
  };

  const end = () => {
    if (timer.value) {
      clearTimeout(timer.value);
      timer.value = null;
    }
  };

  return {
    start,
    end,
    isLongPressing: readonly(isLongPressing),
  };
}
