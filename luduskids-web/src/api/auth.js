import client from './client';

export const register = (email, password) =>
  client.post('/auth/register', { email, password }).then((res) => res.data);

export const login = (email, password) =>
  client.post('/auth/login', { email, password }).then((res) => res.data);

export const getMe = () => client.get('/auth/me').then((res) => res.data);
