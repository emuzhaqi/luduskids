import { Link } from 'react-router-dom';

export default function HomePage() {
  return (
    <div className="home-hero">
      <h1>Welcome to LudusKids</h1>
      <p>Toys that grow imaginations, big and small.</p>
      <Link to="/products" className="button">Shop all toys</Link>
    </div>
  );
}
