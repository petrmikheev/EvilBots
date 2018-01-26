#define COUNT 20

visible int simple[COUNT];
visible int simpleCount = 0;

bool isSimple(int x) {
    for (int i=0; i<simpleCount; ++i)
        if (x % simple[i] == 0) return false;
    return true;
}

int i = 1;
while (simpleCount < COUNT) {
    if (isSimple(++i)) simple[simpleCount++] = i;
}
