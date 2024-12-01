import React, { createContext, useContext, useState, ReactNode } from 'react';

interface Config {
    allowRegistration: boolean;
    enableGoogle: boolean;
    environment: {
        name: string;
        url: string;
        api: string;
        description: string;
    };
}

const ConfigContext = createContext<Config | undefined>(undefined);

export const ConfigProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [config, setConfig] = useState<Config | null>(null);

    // Example: Fetch and set the config at startup
    React.useEffect(() => {
        fetch('http://localhost/SimpleAuthNet/api/AppConfig')
            .then((res) => res.json())
            .then((data) => setConfig(data))
            .catch((error) => console.error('Failed to fetch config:', error));
    }, []);

    if (!config) {
        return <div>Loading configuration...</div>; // Optionally show a loading indicator
    }

    return (
        <ConfigContext.Provider value={config}>
            {children}
        </ConfigContext.Provider>
    );
};

export const useConfig = () => {
    const context = useContext(ConfigContext);
    if (context === undefined) {
        throw new Error('useConfig must be used within a ConfigProvider');
    }
    return context;
};
