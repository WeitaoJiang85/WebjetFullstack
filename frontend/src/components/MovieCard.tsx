import React from "react";
import { Movie } from "../types"; // Ensure you have a Movie type defined
import { FaHeart, FaRegHeart } from "react-icons/fa";

interface MovieCardProps {
  movie: Movie;
  onClick: () => void;
  isSelected: boolean;
  isFavorite: boolean;
  toggleFavorite: () => void;
}

const MovieCard: React.FC<MovieCardProps> = ({
  movie,
  onClick,
  isSelected,
  isFavorite,
  toggleFavorite,
}) => {
  return (
    <div
      onClick={onClick}
      className={`relative flex items-center p-4 rounded-lg transition-transform duration-300 cursor-pointer border border-gray-700 ${
        isSelected ? "transform scale-105 shadow-lg border-yellow-500" : ""
      } hover:scale-105`}
    >
      <img
        src={movie.poster}
        alt={movie.title}
        className="w-16 h-24 object-cover rounded-lg"
      />
      <div className="ml-4 flex-1">
        <h3 className="text-lg font-bold">{movie.title}</h3>
        <p className="text-sm text-gray-400">Year: {movie.year}</p>
      </div>

      {/* Favorite Toggle Button */}
      <button
        onClick={(e) => {
          e.stopPropagation(); // Prevent triggering the card onClick
          toggleFavorite();
        }}
        className="absolute bottom-2 right-2 text-yellow-400"
      >
        {isFavorite ? <FaHeart className="text-red-500" /> : <FaRegHeart />}
      </button>
    </div>
  );
};

export default MovieCard;
