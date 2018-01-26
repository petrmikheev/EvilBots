visible vector v1 = vector(3, 0, 4);
visible vector v2 = v1 + vector(30, 20, 10);
visible vector v3;
visible float d1 = length(v1);
visible float d2 = length(v2);
v3 = rotateRight(normalize(v1), 90);
v1 = rotateUp(v3, 45);
