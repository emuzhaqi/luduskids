import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { createOrder } from '../api/orders';
import { useCart } from '../context/CartContext';

export default function CartPage() {
  const { items, total, updateItem, removeItem, refresh } = useCart();
  const [checkingOut, setCheckingOut] = useState(false);
  const [error, setError] = useState('');
  const navigate = useNavigate();

  const handleCheckout = async () => {
    setError('');
    setCheckingOut(true);
    try {
      const order = await createOrder();
      await refresh();
      navigate(`/orders/${order.id}`);
    } catch {
      setError('Checkout failed. Please try again.');
    } finally {
      setCheckingOut(false);
    }
  };

  if (items.length === 0) {
    return <p>Your cart is empty.</p>;
  }

  return (
    <div className="cart-page">
      <h1>Your cart</h1>
      {error && <p className="form-error">{error}</p>}
      <table>
        <thead>
          <tr>
            <th>Product</th>
            <th>Price</th>
            <th>Quantity</th>
            <th>Subtotal</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{item.productName}</td>
              <td>${item.unitPrice.toFixed(2)}</td>
              <td>
                <input
                  type="number"
                  min={1}
                  value={item.quantity}
                  onChange={(e) => updateItem(item.id, Number(e.target.value))}
                />
              </td>
              <td>${(item.unitPrice * item.quantity).toFixed(2)}</td>
              <td>
                <button onClick={() => removeItem(item.id)}>Remove</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <p className="cart-total">Total: ${total.toFixed(2)}</p>
      <button onClick={handleCheckout} disabled={checkingOut}>
        {checkingOut ? 'Placing order...' : 'Checkout'}
      </button>
    </div>
  );
}
