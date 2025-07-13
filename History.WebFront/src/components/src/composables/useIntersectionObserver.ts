
import { ref, onMounted, onUnmounted } from 'vue';

export function useIntersectionObserver(target: ref, callback: () => void, options = {}) {
  const observer = ref<IntersectionObserver | null>(null);

  const observe = () => {
    if (target.value) {
      observer.value = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            callback();
            unobserve();
          }
        });
      }, options);
      observer.value.observe(target.value);
    }
  };

  const unobserve = () => {
    if (observer.value && target.value) {
      observer.value.unobserve(target.value);
      observer.value = null;
    }
  };

  onMounted(observe);
  onUnmounted(unobserve);

  return {
    observe,
    unobserve,
  };
}
