import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';

export default function Navbar() {
  const { user, logout } = useAuth();
  const { items } = useCart();
  const navigate = useNavigate();

  const itemCount = items.reduce((sum, item) => sum + item.quantity, 0);

  const handleLogout = () => {
    logout();
    navigate('/');
  };

  return (
    <nav className="navbar">
      <Link to="/" className="navbar-brand">LudusKids</Link>
      <div className="navbar-links">
        <Link to="/products">Shop</Link>
        {user && <Link to="/cart">Cart ({itemCount})</Link>}
        {user && <Link to="/orders">My Orders</Link>}
        {user?.role === 'Admin' && <Link to="/admin/products">Manage Products</Link>}
        {user?.role === 'Admin' && <Link to="/admin/orders">Manage Orders</Link>}
        {user ? (
          <>
            <span className="navbar-user">{user.email}</span>
            <button onClick={handleLogout}>Log out</button>
          </>
        ) : (
          <>
            <Link to="/login">Log in</Link>
            <Link to="/register">Register</Link>
          </>
        )}
      </div>
    </nav>
  );
}
