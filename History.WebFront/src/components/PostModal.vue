<!-- src/components/PostModal.vue -->
<template>
    <div class="modal-overlay" @click.self="close">
      <div class="modal-content">
        <CreatePost @post-created="close" />
      </div>
    </div>
  </template>
  
  <script setup>
  import { useUiStore } from '@/stores/ui';
  import CreatePost from './CreatePost.vue';
  
  const uiStore = useUiStore();
  
  const close = () => {
    uiStore.closePostEditor();  // 모달 닫기
  };
  </script>
  
  <style scoped>
  .modal-overlay {
    position: fixed;
    inset: 0;
    background-color: rgba(0, 0, 0, 0.4);
    z-index: 9999;
    display: flex;
    justify-content: center;
    align-items: center;
  }
  
  .modal-content {
    background: white;
    width: 600px;
    max-height: 90vh;
    overflow-y: auto;
    padding: 20px;
    border-radius: 12px;
    box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
  }
  </style>
  