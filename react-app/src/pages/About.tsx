import React, { useState } from 'react';
import { useConfig } from '../contexts/ConfigContext';

const About: React.FC = () => {
  const config = useConfig();
  const [hasPref, setHasPref] = useState(!!localStorage.getItem('preferredMfaMethod'));

  const clearPref = () => {
    if (window.confirm('Are you sure you want to clear your MFA preference?')) {
      localStorage.removeItem('preferredMfaMethod');
      setHasPref(false);
    }
  };

  return (
    <div className="container">
      <h2>About</h2>
      <p>{config.environment.description}</p>
      {hasPref && (
        <button className="btn btn-warning" onClick={clearPref}>
          Clear MFA Preference
        </button>
      )}
    </div>
  );
};

export default About;
