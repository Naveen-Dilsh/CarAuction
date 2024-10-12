import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import axios from 'axios';

const VerifyEmail = () => {
  const [verificationStatus, setVerificationStatus] = useState('Verifying...');
  const location = useLocation();

  useEffect(() => {
    const searchParams = new URLSearchParams(location.search);
    const token = searchParams.get('token');

    if (token) {
      verifyEmail(token);
    } else {
      setVerificationStatus('Invalid verification link.');
    }
  }, [location]);

  const verifyEmail = async (token) => {
  try {
    const response = await axios.get(`https://localhost:7021/api/Auth/verify-email?token=${token}`);
    console.log('Verification response:', response);
    setVerificationStatus(response.data.Message);
  } catch (error) {
    console.error('Verification error:', error);
    if (error.response) {
      console.error('Error response:', error.response);
      console.error('Error response data:', error.response.data);
      console.error('Error response status:', error.response.status);
      console.error('Error response headers:', error.response.headers);
    } else if (error.request) {
      console.error('Error request:', error.request);
    } else {
      console.error('Error message:', error.message);
    }
    setVerificationStatus(error.response?.data?.Message || 'Email verification failed.');
  }
};

  return (
    <div>
      <h2>Email Verification</h2>
      <p>{verificationStatus}</p>
    </div>
  );
};

export default VerifyEmail;