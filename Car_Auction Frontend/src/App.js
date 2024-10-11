import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import Home from './Components/Home';
import Register from './Components/Register';
import VerifyEmail from './Components/VerifyEmail';

function App() {
  return (
    <Router>
      <div className="App">
        <Routes>
          <Route path="/" element={<Home />} /> {/* Home route */}
          <Route path="/register" element={<Register />} /> {/* Registration route */}
          <Route path="/verify-email" element={<VerifyEmail />} /> {/* Email verification route */}
        </Routes>
      </div>
    </Router>
  );
}

export default App;
