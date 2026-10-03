using OpenTK.Mathematics;

namespace PVZRemake.Board
{
    public static class Grid
    {
        private static BoardType _currentType = BoardType.Day;
        private static BoardLayout _currentLayout;

        static Grid()
        {
            // Инициализируем дефолтный тип
            SetBoardType(BoardType.Day);
        }

        /// <summary>
        /// Переключает геометрию сетки под конкретный тип уровня
        /// </summary>
        public static void SetBoardType(BoardType type)
        {
            _currentType = type;

            switch (type)
            {
                case BoardType.Day:
                case BoardType.Night:
                    _currentLayout = new BoardLayout
                    {
                        Rows = 5,
                        StartX = 280f,
                        StartY = 130f,
                        CellWidth = 121.1f,
                        CellHeight = 146f, // В бассейне линии обычно чуть плотнее
                        LaneTypes = [LaneType.Normal, LaneType.Normal, LaneType.Normal, LaneType.Normal, LaneType.Normal]
                    };
                    break;

                case BoardType.Pool:
                case BoardType.Fog:
                    _currentLayout = new BoardLayout
                    {
                        Rows = 6,
                        StartX = 280f,
                        StartY = 130f,
                        CellWidth = 121f,
                        CellHeight = 121.7f, // В бассейне линии обычно чуть плотнее
                        LaneTypes = [LaneType.Normal, LaneType.Normal, LaneType.Pool, LaneType.Pool, LaneType.Normal, LaneType.Normal]
                    };
                    break;

                case BoardType.Roof:
                    _currentLayout = new BoardLayout
                    {
                        Rows = 5,
                        StartX = 280f,
                        StartY = 130f,
                        CellWidth = 121f,
                        CellHeight = 146f, // В бассейне линии обычно чуть плотнее
                        LaneTypes = [LaneType.Roof, LaneType.Roof, LaneType.Roof, LaneType.Roof, LaneType.Roof]
                    };
                    break;
            }
        }

        public static int TotalRows => _currentLayout.Rows;
        public static int TotalCols => 9; // В PvZ всегда 9 вертикальных игровых колонок

        /// <summary>
        /// Получить тип дорожки по индексу строки
        /// </summary>
        public static LaneType GetLaneType(int row)
        {
            if (row < 0 || row >= _currentLayout.Rows) return LaneType.Normal;
            return _currentLayout.LaneTypes[row];
        }

        /// <summary>
        /// Перевод пикселей мыши в логические координаты ячейки
        /// </summary>
        public static GridCoords? GetCoordsFromScreen(Vector2 screenPos)
        {
            float localX = screenPos.X - _currentLayout.StartX;
            float localY = screenPos.Y - _currentLayout.StartY;

            if (localX < 0 || localY < 0) return null;

            int col = (int)(localX / _currentLayout.CellWidth);
            int row = (int)(localY / _currentLayout.CellHeight);

            // Проверка на выход за границы игрового поля
            if (row < 0 || row >= _currentLayout.Rows || col < 0 || col >= 9) return null;

            // Считаем точные мировые/экранные координаты для этой ячейки
            float cellX = _currentLayout.StartX + (col * _currentLayout.CellWidth);
            float cellY = _currentLayout.StartY + (row * _currentLayout.CellHeight);

            // На крыше можно будет добавить смещение по Y (в зависимости от колонки col), чтобы симулировать наклон!
            if (_currentType == BoardType.Roof)
            {
                // Пример: чем левее колонка, тем выше ячейка (наклон крыши)
                // cellY += (5 - col) * 10f; 
            }

            Vector2 topLeft = new(cellX, cellY);
            Vector2 center = new(cellX + _currentLayout.CellWidth * 0.5f, cellY + _currentLayout.CellHeight * 0.5f);

            return new GridCoords(row, col, topLeft, center);
        }

        /// <summary>
        /// Получить координаты ячейки по известным Row и Col (например, для спавна из кода)
        /// </summary>
        public static GridCoords GetCoords(int row, int col)
        {
            row = Math.Clamp(row, 0, _currentLayout.Rows - 1);
            col = Math.Clamp(col, 0, 8);

            float cellX = _currentLayout.StartX + (col * _currentLayout.CellWidth);
            float cellY = _currentLayout.StartY + (row * _currentLayout.CellHeight);

            Vector2 topLeft = new(cellX, cellY);
            Vector2 center = new(cellX + _currentLayout.CellWidth * 0.5f, cellY + _currentLayout.CellHeight * 0.5f);

            return new GridCoords(row, col, topLeft, center);
        }
    }
}
