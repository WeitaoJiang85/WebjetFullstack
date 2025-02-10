import React, { useState } from "react";
import { MenuItem, Select, FormControl, InputLabel } from "@mui/material";

interface SortFilterProps {
  onSortChange: (sortType: string, sortOrder: "asc" | "desc") => void;
}

const SortFilter: React.FC<SortFilterProps> = ({ onSortChange }) => {
  const [sortOption, setSortOption] = useState("rating-desc"); // 默认按评分降序

  const handleSortChange = (event: any) => {
    const selectedOption = event.target.value;
    setSortOption(selectedOption);

    const [type, order] = selectedOption.split("-");
    onSortChange(type, order as "asc" | "desc");
  };

  return (
    <FormControl
      fullWidth
      className="bg-gray-900 text-white rounded-lg shadow-md"
    >
      <InputLabel className="text-white">Sort by</InputLabel>
      <Select
        value={sortOption}
        onChange={handleSortChange}
        className="bg-gray-800 text-white"
      >
        <MenuItem value="year-desc">Year (New → Old)</MenuItem>
        <MenuItem value="year-asc">Year (Old → New)</MenuItem>
        <MenuItem value="rating-desc">Rating (High → Low)</MenuItem>
        <MenuItem value="rating-asc">Rating (Low → High)</MenuItem>
        <MenuItem value="votes-desc">Votes (High → Low)</MenuItem>
        <MenuItem value="votes-asc">Votes (Low → High)</MenuItem>
        <MenuItem value="bestPrice-asc">Best Price (Low → High)</MenuItem>
        <MenuItem value="bestPrice-desc">Best Price (High → Low)</MenuItem>
      </Select>
    </FormControl>
  );
};

export default SortFilter;
