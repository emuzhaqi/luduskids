import client from './client';

export const createOrder = () => client.post('/orders').then((res) => res.data);

export const getOrders = () => client.get('/orders').then((res) => res.data);

export const getOrder = (id) => client.get(`/orders/${id}`).then((res) => res.data);

export const updateOrderStatus = (id, status) =>
  client.put(`/orders/${id}/status`, { status }).then((res) => res.data);
