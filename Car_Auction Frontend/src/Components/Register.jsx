import React, { useState } from 'react';
import axios from 'axios';
import { useNavigate } from 'react-router-dom'; // Import useNavigate

const RegistrationForm = () => {
  const navigate = useNavigate(); // Initialize useNavigate
  const [formData, setFormData] = useState({
    UName: '',
    UEmail: '',
    UPassword: '',
    URole: ''
  });
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);  // Add success state

  const handleChange = (e) => {
    setFormData({
      ...formData,
      [e.target.name]: e.target.value
    });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess(false);  // Reset success on form submission
    
    try {
      console.log(formData);
      const response = await axios.post('https://localhost:7021/api/Auth/register', formData);
      setSuccess(true);  // Set success to true if registration is successful
    } catch (err) {
      setError(err.response ? err.response.data.Message : 'Something went wrong.');
    }
  };

  const handleLoginRedirect = () => {
    navigate('/login'); // Navigate to the login component
  };

  return (
    <div>
      <h2>Register</h2>
      {error && <div style={{ color: 'red' }}>{error}</div>}
      {success && <div style={{ color: 'green' }}>Registration successful!</div>}  {/* Success message */}
      
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="UName">Username:</label>
          <input
            type="text"
            id="UName"
            name="UName"
            value={formData.UName}
            onChange={handleChange}
            required
          />
        </div>
        <div>
          <label htmlFor="UEmail">Email:</label>
          <input
            type="email"
            id="UEmail"
            name="UEmail"
            value={formData.UEmail}
            onChange={handleChange}
            required
          />
        </div>
        <div>
          <label htmlFor="UPassword">Password:</label>
          <input
            type="password"
            id="UPassword"
            name="UPassword"
            value={formData.UPassword}
            onChange={handleChange}
            required
          />
        </div>
        <div>
          <label htmlFor="URole">Role:</label>
          <select
            id="URole"
            name="URole"
            value={formData.URole}
            onChange={handleChange}
            required
          >
            <option value="">Select Role</option>
            <option value="user">User</option>
            <option value="admin">Admin</option>
          </select>
        </div>
        <button type="submit">Register</button>
      </form>
      
      <button type="button" onClick={handleLoginRedirect}>
          Already have an account? Login
      </button>
    </div>
  );
};

export default RegistrationForm;
