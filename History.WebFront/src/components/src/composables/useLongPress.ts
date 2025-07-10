import { ref, readonly } from 'vue';

export function useLongPress(delay: number = 500) {
  const timer = ref<ReturnType<typeof setTimeout> | null>(null);
  const isLongPressing = ref(false);

  const start = (callback: () => void) => {
    console.log('[useLongPress] start: 타이머 설정 시도');
    isLongPressing.value = false;
    if (timer.value) {
      clearTimeout(timer.value);
    }
    timer.value = setTimeout(() => {
      console.log('[useLongPress] long press! 콜백 실행');
      isLongPressing.value = true;
      callback();
      timer.value = null;
    }, delay);
  };

  const end = () => {
    if (timer.value) {
      console.log('[useLongPress] end: 타이머 취소 (일반 클릭으로 간주)');
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
