import React from 'react';
import { useNavigate } from 'react-router-dom';

const Home = () => {
  const navigate = useNavigate();

  const handleRegisterClick = () => {
    navigate('/register'); // Redirect to the registration page
  };

  return (
    <div>
      <h1>Welcome to Car Auction</h1>
      <button onClick={handleRegisterClick}>
        Register Now
      </button>
    </div>
  );
};

export default Home;
