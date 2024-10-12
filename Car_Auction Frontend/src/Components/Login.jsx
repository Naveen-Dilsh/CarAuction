import React, { useState } from 'react';
import axios from 'axios';

const Login = () => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoggedIn, setIsLoggedIn] = useState(false);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    try {
      console.log('Attempting login with:', { username, password });
      const response = await axios.post('https://localhost:7021/api/Auth/login', { username, password });

      console.log('Login response:', response);

      if (response.data) {
        console.log('Response data:', response.data);
        setIsLoggedIn(true);
        // You can add additional logic here, such as storing user info in state or local storage
      } else {
        console.warn('No data received in the response');
        setError('Login failed. No data received from server.');
      }
    } catch (err) {
      console.error('Login error:', err);
      
      if (err.response) {
        console.error('Error response:', err.response);
        setError(`Login failed: ${err.response.data.message || err.response.statusText}`);
      } else if (err.request) {
        console.error('No response received:', err.request);
        setError('No response received from the server. Please check your internet connection and try again.');
      } else {
        console.error('Error details:', err);
        setError(`An unexpected error occurred: ${err.message}`);
      }
    }
  };

  return (
    <div className="login-container">
      <h2>Login</h2>
      {isLoggedIn ? (
        <p>You are logged in successfully!</p>
      ) : (
        <form onSubmit={handleSubmit}>
          <div>
            <label htmlFor="username">Username:</label>
            <input
              type="text"
              id="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              required
            />
          </div>
          <div>
            <label htmlFor="password">Password:</label>
            <input
              type="password"
              id="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
            />
          </div>
          {error && <p className="error">{error}</p>}
          <button type="submit">Login</button>
        </form>
      )}
    </div>
  );
};

export default Login;