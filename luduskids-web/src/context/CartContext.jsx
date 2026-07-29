import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import * as cartApi from '../api/cart';
import { useAuth } from './AuthContext';

const CartContext = createContext(null);

export function CartProvider({ children }) {
  const { user } = useAuth();
  const [items, setItems] = useState([]);

  const refresh = useCallback(async () => {
    if (!user) {
      setItems([]);
      return;
    }
    setItems(await cartApi.getCart());
  }, [user]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const addItem = async (productId, quantity = 1) => {
    await cartApi.addCartItem(productId, quantity);
    await refresh();
  };

  const updateItem = async (id, quantity) => {
    await cartApi.updateCartItem(id, quantity);
    await refresh();
  };

  const removeItem = async (id) => {
    await cartApi.removeCartItem(id);
    await refresh();
  };

  const total = items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);

  return (
    <CartContext.Provider value={{ items, total, addItem, updateItem, removeItem, refresh }}>
      {children}
    </CartContext.Provider>
  );
}

export function useCart() {
  return useContext(CartContext);
}
