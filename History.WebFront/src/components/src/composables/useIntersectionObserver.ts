
import { ref, onMounted, onUnmounted, watch } from 'vue';

export function useIntersectionObserver(target: ref, callback: () => void, options = {}) {
  const observer = ref<IntersectionObserver | null>(null);

  const setupObserver = () => {
    if (target.value && target.value instanceof HTMLElement && !observer.value) {
      observer.value = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            callback();
            cleanupObserver(); // Once it intersects, we can stop observing
          }
        });
      }, options);
      observer.value.observe(target.value);
    }
  };

  const cleanupObserver = () => {
    if (observer.value && target.value) {
      observer.value.unobserve(target.value);
      observer.value.disconnect(); // Disconnect the observer
      observer.value = null;
    }
  };

  onUnmounted(cleanupObserver);

  watch(target, (newValue) => {
    if (newValue) {
      setupObserver();
    } else {
      cleanupObserver();
    }
  }, { immediate: true });

  return {
    setupObserver,
    cleanupObserver,
  };
}
