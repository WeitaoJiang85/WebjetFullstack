import { Button, Card, CardContent, Typography } from "@mui/material";

export default function App() {
  return (
    <div className="flex flex-col items-center justify-center min-h-screen gap-4 bg-gray-100">
      <Card className="w-96 shadow-lg">
        <CardContent>
          <Typography variant="h5" component="div" className="text-blue-500">
            Material UI + Tailwind CSS 🚀
          </Typography>
          <Typography variant="body2" color="text.secondary">
            这是一张示例卡片，展示了 MUI 和 Tailwind 的组合使用。
          </Typography>
          <div className="mt-4 flex justify-center">
            <Button variant="contained" color="primary">
              测试按钮
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
