int f(int x, int y) {
	return x + y;
}
visible int g[2];
g[0] = f(2, 3);
g[1] = f(g[0]++, 4);
