import React, { useContext } from "react";
import { Typography, Card, CardMedia, CardContent, Box } from "@mui/material";
import { GiPriceTag } from "react-icons/gi";
import { MovieContext } from "../context/MovieContext";

const MovieDetail: React.FC = () => {
  const context = useContext(MovieContext);
  if (!context) return null;

  const { selectedMovie } = context;

  if (!selectedMovie) {
    return (
      <Box className="flex justify-center items-center h-full text-gray-400">
        <Typography variant="h5">Select a movie to see details</Typography>
      </Box>
    );
  }

  return (
    <Card className="bg-gray-900 text-white w-full h-full p-4 shadow-lg">
      {/* Movie Poster */}
      <CardMedia
        component="img"
        height="300"
        image={selectedMovie.poster}
        alt={selectedMovie.title}
        className="rounded-lg"
      />

      <CardContent>
        <Typography variant="h4" className="font-bold text-yellow-400 mb-2">
          {selectedMovie.title}
        </Typography>

        <Typography variant="body1" className="text-gray-300">
          <strong>Year:</strong> {selectedMovie.year}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Rated:</strong> {selectedMovie.rated}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Released:</strong> {selectedMovie.released}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Runtime:</strong> {selectedMovie.runtime}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Genre:</strong> {selectedMovie.genre}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Director:</strong> {selectedMovie.director}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Writer:</strong> {selectedMovie.writer}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Actors:</strong> {selectedMovie.actors}
        </Typography>
        <Typography variant="body1" className="text-gray-300">
          <strong>Plot:</strong> {selectedMovie.plot}
        </Typography>

        {/* Pricing */}
        <Box className="mt-4">
          <Typography
            variant="h5"
            className="flex items-center text-yellow-400 font-bold"
          >
            <GiPriceTag className="mr-2" /> Best Price: $
            {selectedMovie.firstPrice} ({selectedMovie.firstProvider})
          </Typography>

          <Typography
            variant="h6"
            className={`flex items-center text-gray-300 transition-opacity duration-500 ${
              selectedMovie.secondProvider === "Unknown"
                ? "opacity-0"
                : "opacity-100"
            }`}
          >
            <GiPriceTag className="mr-2" /> Other Price: $
            {selectedMovie.secondPrice} ({selectedMovie.secondProvider})
          </Typography>
        </Box>
      </CardContent>
    </Card>
  );
};

export default MovieDetail;
