import React from 'react';
import { useAuth } from './authContext';
import axios from 'axios';

const ProtectedComponent = () => {
  const { token } = useAuth();

  const fetchProtectedData = async () => {
    try {
      const response = await axios.get('https://localhost:7021/api/protected-route', {
        headers: { Authorization: `Bearer ${token}` }
      });
      console.log(response.data);
    } catch (error) {
      console.error('Error fetching protected data:', error);
    }
  };

  return (
    <div>
      <h2>Protected Component</h2>
      <button onClick={fetchProtectedData}>Fetch Protected Data</button>
    </div>
  );
};

export default ProtectedComponent;