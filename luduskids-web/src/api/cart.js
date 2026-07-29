import client from './client';

export const getCart = () => client.get('/cart/items').then((res) => res.data);

export const addCartItem = (productId, quantity) =>
  client.post('/cart/items', { productId, quantity }).then((res) => res.data);

export const updateCartItem = (id, quantity) =>
  client.put(`/cart/items/${id}`, { quantity }).then((res) => res.data);

export const removeCartItem = (id) => client.delete(`/cart/items/${id}`).then((res) => res.data);
