<template>
  <Teleport to="body">
    <div v-if="show" class="modal-overlay" @click.self="$emit('close')">
      <div class="modal-content">
        <h3>Users</h3>
        <ul>
          <li v-for="user in users" :key="user.id">{{ user.nickname }}</li>
        </ul>
        <button @click="$emit('close')">Close</button>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
interface User {
  id: string;
  nickname: string;
}

const props = defineProps({
  show: {
    type: Boolean,
    required: true,
  },
  users: {
    type: Array as () => User[],
    default: () => [],
  },
});

const emit = defineEmits(['close']);
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
}

ul {
  list-style: none;
  padding: 0;
}

li {
  padding: 8px 0;
  border-bottom: 1px solid #eee;
}

li:last-child {
  border-bottom: none;
}

button {
  margin-top: 20px;
  padding: 10px 15px;
  background-color: #007bff;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
}
</style>
