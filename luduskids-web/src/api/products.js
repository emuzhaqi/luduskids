import client from './client';

export const getProducts = () => client.get('/products').then((res) => res.data);

export const getProduct = (id) => client.get(`/products/${id}`).then((res) => res.data);

export const createProduct = (product) => client.post('/products', product).then((res) => res.data);

export const updateProduct = (id, product) => client.put(`/products/${id}`, product).then((res) => res.data);

export const deleteProduct = (id) => client.delete(`/products/${id}`).then((res) => res.data);

export const getCategories = () => client.get('/categories').then((res) => res.data);

export const createCategory = (category) => client.post('/categories', category).then((res) => res.data);

export const updateCategory = (id, category) => client.put(`/categories/${id}`, category).then((res) => res.data);

export const deleteCategory = (id) => client.delete(`/categories/${id}`).then((res) => res.data);
