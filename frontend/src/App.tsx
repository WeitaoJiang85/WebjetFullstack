import React from "react";
import { MovieProvider } from "./context/MovieContext";
import Home from "./pages/Home";

const App: React.FC = () => {
  return (
    <MovieProvider>
      <Home />
    </MovieProvider>
  );
};

export default App;
