<template>
  <Teleport to="body">
    <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
      <div class="modal-content">
        <h3>Report Post</h3>
        <textarea v-model="reportReason" placeholder="Reason for reporting..."></textarea>
        <button @click="submitReport">Submit Report</button>
        <button @click="$emit('close')">Cancel</button>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref } from 'vue';

const props = defineProps({
  show: {
    type: Boolean,
    required: true,
  },
  postId: {
    type: String,
    required: true,
  },
});

const emit = defineEmits(['close', 'report-submitted']);

const reportReason = ref('');

const submitReport = () => {
  if (reportReason.value.trim()) {
    console.log(`Reporting post ${props.postId} for: ${reportReason.value}`);
    // Here you would typically call an API to submit the report
    emit('report-submitted', props.postId, reportReason.value);
    emit('close');
    reportReason.value = '';
  }
};
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  padding: 20px;
  border-radius: 8px;
  max-width: 500px;
  width: 90%;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

textarea {
  width: 100%;
  height: 100px;
  padding: 10px;
  border: 1px solid #ccc;
  border-radius: 5px;
  resize: vertical;
}

button {
  padding: 10px 15px;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}

button:last-of-type {
  background-color: #6c757d;
}
</style>
